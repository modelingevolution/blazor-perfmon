using System.Threading.Tasks.Dataflow;
using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Server.Services;

/// <summary>
/// TPL Dataflow pipeline for metrics multiplexing.
/// Stage 4: Uses nested JoinBlocks to combine (CPU+GPU+RAM), Network (with collection time), Disk, and Docker data.
/// Tracks connected clients and fires events when first client connects or last client disconnects.
/// </summary>
public sealed class MultiplexService : IDisposable
{
    private readonly BufferBlock<(float[] CpuLoads, float[] GpuLoads, RamMetric Ram, uint TimestampMs)> _cpuGpuBuffer;
    private readonly BufferBlock<(NetworkMetric[] Metrics, uint CollectionTimeMs)> _networkBuffer;
    private readonly BufferBlock<DiskMetric[]> _diskBuffer;
    private readonly BufferBlock<DockerContainerMetric[]> _dockerBuffer;
    private readonly JoinBlock<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]> _firstJoinBlock;
    private readonly JoinBlock<Tuple<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]>, DockerContainerMetric[]> _secondJoinBlock;
    private readonly TransformBlock<Tuple<Tuple<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]>, DockerContainerMetric[]>, byte[]> _serializeBlock;
    private readonly BroadcastBlock<byte[]> _broadcastBlock;

    private int _clientCount = 0;

    /// <summary>
    /// Fired when the first client connects (transition from 0 to 1 clients).
    /// </summary>
    public event Action? FirstClientConnected;

    /// <summary>
    /// Fired when the last client disconnects (transition from 1 to 0 clients).
    /// </summary>
    public event Action? LastClientDisconnected;

    public MultiplexService()
    {
        // Stage 4: Separate buffers for CPU+GPU+RAM (with timestamp), Network (with collection time), Disk, and Docker
        _cpuGpuBuffer = new BufferBlock<(float[], float[], RamMetric, uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _networkBuffer = new BufferBlock<(NetworkMetric[], uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _diskBuffer = new BufferBlock<DiskMetric[]>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _dockerBuffer = new BufferBlock<DockerContainerMetric[]>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // First JoinBlock: Wait for CPU+GPU+RAM (with timestamp), Network (with collection time), and Disk
        _firstJoinBlock = new JoinBlock<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]>(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // Second JoinBlock: Wait for first join result and Docker data
        _secondJoinBlock = new JoinBlock<Tuple<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]>, DockerContainerMetric[]>(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // Transform block: Serialize combined data to MessagePack
        _serializeBlock = new TransformBlock<Tuple<Tuple<(float[], float[], RamMetric, uint), (NetworkMetric[], uint), DiskMetric[]>, DockerContainerMetric[]>, byte[]>(
            combinedData =>
            {
                var (firstJoinResult, dockerMetrics) = combinedData;
                var (cpuGpuRamData, networkData, diskMetrics) = firstJoinResult;
                var (cpuLoads, gpuLoads, ram, timestampMs) = cpuGpuRamData;
                var (networkMetrics, collectionTimeMs) = networkData;

                // Convert server-side NetworkMetric to shared NetworkMetric for serialization
                var sharedNetworkMetrics = networkMetrics.Select(n => new NetworkMetric
                {
                    Identifier = n.Identifier,
                    RxBytes = n.RxBytes,
                    TxBytes = n.TxBytes
                }).ToArray();

                var sample = new MetricSample
                {
                    CreatedAt = timestampMs,
                    GpuLoads = gpuLoads,
                    CpuLoads = cpuLoads,
                    Ram = new RamMetric
                    {
                        UsedBytes = ram.UsedBytes,
                        TotalBytes = ram.TotalBytes
                    },
                    DiskMetrics = diskMetrics,
                    NetworkMetrics = sharedNetworkMetrics,
                    DockerContainers = dockerMetrics,
                    CollectionDurationMs = collectionTimeMs
                };

                return MessagePackSerializer.Serialize(sample);
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 2,
                SingleProducerConstrained = true
            });

        // Broadcast block: Send serialized data to all connected clients
        _broadcastBlock = new BroadcastBlock<byte[]>(bytes => bytes);

        // Link pipeline: (CPU+GPU)/Network/Disk -> FirstJoin -> (FirstJoin result + Docker) -> SecondJoin -> Serialize -> Broadcast
        _cpuGpuBuffer.LinkTo(_firstJoinBlock.Target1, new DataflowLinkOptions { PropagateCompletion = true });
        _networkBuffer.LinkTo(_firstJoinBlock.Target2, new DataflowLinkOptions { PropagateCompletion = true });
        _diskBuffer.LinkTo(_firstJoinBlock.Target3, new DataflowLinkOptions { PropagateCompletion = true });
        _firstJoinBlock.LinkTo(_secondJoinBlock.Target1, new DataflowLinkOptions { PropagateCompletion = true });
        _dockerBuffer.LinkTo(_secondJoinBlock.Target2, new DataflowLinkOptions { PropagateCompletion = true });
        _secondJoinBlock.LinkTo(_serializeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _serializeBlock.LinkTo(_broadcastBlock, new DataflowLinkOptions { PropagateCompletion = true });
    }

    /// <summary>
    /// Post CPU, GPU, and RAM metrics to the pipeline with timestamp.
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostCpuGpuRamMetrics(float[] cpuData, float[] gpuLoads, RamMetric ram, uint timestampMs)
    {
        return _cpuGpuBuffer.Post((cpuData, gpuLoads, ram, timestampMs));
    }

    /// <summary>
    /// Post Network metrics to the pipeline (includes collection time).
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostNetworkMetrics(NetworkMetric[] metrics, uint collectionTimeMs)
    {
        return _networkBuffer.Post((metrics, collectionTimeMs));
    }

    /// <summary>
    /// Post Disk metrics to the pipeline.
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostDiskMetrics(DiskMetric[] diskMetrics)
    {
        return _diskBuffer.Post(diskMetrics);
    }

    /// <summary>
    /// Post Docker container metrics to the pipeline.
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostDockerMetrics(DockerContainerMetric[] dockerMetrics)
    {
        return _dockerBuffer.Post(dockerMetrics);
    }

    /// <summary>
    /// Create a target block that receives broadcasted metrics.
    /// Each WebSocket client should create its own target block.
    /// Fires FirstClientConnected event when transitioning from 0 to 1 clients.
    /// </summary>
    public ITargetBlock<byte[]> CreateClientTarget(Func<byte[], Task> sendAction)
    {
        var actionBlock = new ActionBlock<byte[]>(
            sendAction,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 2,
                MaxDegreeOfParallelism = 1
            });

        // Link broadcast to this client's action block
        _broadcastBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = false });

        // Track client count using Interlocked
        var newCount = Interlocked.Increment(ref _clientCount);
        if (newCount == 1)
        {
            FirstClientConnected?.Invoke();
        }

        return actionBlock;
    }

    /// <summary>
    /// Unlink a client target from the broadcast block.
    /// Call this when a WebSocket client disconnects.
    /// Fires LastClientDisconnected event when transitioning from 1 to 0 clients.
    /// </summary>
    public void UnlinkClientTarget(ITargetBlock<byte[]> target)
    {
        // Unlinking is automatic when the target completes
        // Just complete the target block
        target.Complete();

        // Track client count using Interlocked
        var newCount = Interlocked.Decrement(ref _clientCount);
        if (newCount == 0)
        {
            LastClientDisconnected?.Invoke();
        }
    }

    public void Dispose()
    {
        _cpuGpuBuffer.Complete();
        _networkBuffer.Complete();
        _diskBuffer.Complete();
        _dockerBuffer.Complete();
        _firstJoinBlock.Complete();
        _secondJoinBlock.Complete();
        _serializeBlock.Complete();
        _broadcastBlock.Complete();
    }
}
