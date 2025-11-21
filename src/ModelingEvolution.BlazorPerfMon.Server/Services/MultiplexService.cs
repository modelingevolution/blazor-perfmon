using System.Threading.Tasks.Dataflow;
using Backend.Core;
using Backend.Collectors;
using MessagePack;

namespace Backend.Services;

/// <summary>
/// TPL Dataflow pipeline for metrics multiplexing.
/// Stage 4: Uses 3-way JoinBlock to combine (CPU+GPU+RAM), Network (with collection time), and Disk data.
/// </summary>
public sealed class MultiplexService : IDisposable
{
    private readonly BufferBlock<(float[] CpuLoads, float[] GpuLoads, RamMetrics Ram, uint TimestampMs)> _cpuGpuBuffer;
    private readonly BufferBlock<(NetworkMetric[] Metrics, uint CollectionTimeMs)> _networkBuffer;
    private readonly BufferBlock<(ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops)> _diskBuffer;
    private readonly JoinBlock<(float[], float[], RamMetrics, uint), (NetworkMetric[], uint), (ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops)> _joinBlock;
    private readonly TransformBlock<Tuple<(float[], float[], RamMetrics, uint), (NetworkMetric[], uint), (ulong, ulong, uint, uint)>, byte[]> _serializeBlock;
    private readonly BroadcastBlock<byte[]> _broadcastBlock;

    public MultiplexService()
    {
        // Stage 4: Separate buffers for CPU+GPU+RAM (with timestamp), Network (with collection time), and Disk
        _cpuGpuBuffer = new BufferBlock<(float[], float[], RamMetrics, uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _networkBuffer = new BufferBlock<(NetworkMetric[], uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _diskBuffer = new BufferBlock<(ulong, ulong, uint, uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // JoinBlock: Wait for CPU+GPU+RAM (with timestamp), Network (with collection time), and Disk data before proceeding
        _joinBlock = new JoinBlock<(float[], float[], RamMetrics, uint), (NetworkMetric[], uint), (ulong, ulong, uint, uint)>(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // Transform block: Serialize combined data to MessagePack
        _serializeBlock = new TransformBlock<Tuple<(float[], float[], RamMetrics, uint), (NetworkMetric[], uint), (ulong, ulong, uint, uint)>, byte[]>(
            combinedData =>
            {
                var (cpuGpuRamData, networkData, diskData) = combinedData;
                var (cpuLoads, gpuLoads, ram, timestampMs) = cpuGpuRamData;
                var (networkMetrics, collectionTimeMs) = networkData;
                var (readBytes, writeBytes, readIops, writeIops) = diskData;

                // Convert server-side NetworkMetric to shared NetworkMetric for serialization
                var sharedNetworkMetrics = networkMetrics.Select(n => new Frontend.Models.NetworkMetric
                {
                    Identifier = n.Identifier,
                    RxBytes = n.RxBytes,
                    TxBytes = n.TxBytes
                }).ToArray();

                var sample = new Frontend.Models.MetricSample
                {
                    CreatedAt = timestampMs,
                    GpuLoads = gpuLoads,
                    CpuLoads = cpuLoads,
                    Ram = new Frontend.Models.RamMetric
                    {
                        UsedBytes = ram.UsedBytes,
                        TotalBytes = ram.TotalBytes
                    },
                    DiskMetrics = new[]
                    {
                        new Frontend.Models.DiskMetric
                        {
                            ReadBytes = readBytes,
                            WriteBytes = writeBytes,
                            ReadIops = readIops,
                            WriteIops = writeIops
                        }
                    },
                    NetworkMetrics = sharedNetworkMetrics,
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

        // Link pipeline: (CPU+GPU)/Network/Disk -> Join -> Serialize -> Broadcast
        _cpuGpuBuffer.LinkTo(_joinBlock.Target1, new DataflowLinkOptions { PropagateCompletion = true });
        _networkBuffer.LinkTo(_joinBlock.Target2, new DataflowLinkOptions { PropagateCompletion = true });
        _diskBuffer.LinkTo(_joinBlock.Target3, new DataflowLinkOptions { PropagateCompletion = true });
        _joinBlock.LinkTo(_serializeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _serializeBlock.LinkTo(_broadcastBlock, new DataflowLinkOptions { PropagateCompletion = true });
    }

    /// <summary>
    /// Post CPU, GPU, and RAM metrics to the pipeline with timestamp.
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostCpuGpuRamMetrics(float[] cpuData, float[] gpuLoads, RamMetrics ram, uint timestampMs)
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
    public bool PostDiskMetrics(ulong readBytes, ulong writeBytes, uint readIops, uint writeIops)
    {
        return _diskBuffer.Post((readBytes, writeBytes, readIops, writeIops));
    }

    /// <summary>
    /// Create a target block that receives broadcasted metrics.
    /// Each WebSocket client should create its own target block.
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

        return actionBlock;
    }

    /// <summary>
    /// Unlink a client target from the broadcast block.
    /// Call this when a WebSocket client disconnects.
    /// </summary>
    public void UnlinkClientTarget(ITargetBlock<byte[]> target)
    {
        // Unlinking is automatic when the target completes
        // Just complete the target block
        target.Complete();
    }

    public void Dispose()
    {
        _cpuGpuBuffer.Complete();
        _networkBuffer.Complete();
        _diskBuffer.Complete();
        _joinBlock.Complete();
        _serializeBlock.Complete();
        _broadcastBlock.Complete();
    }
}
