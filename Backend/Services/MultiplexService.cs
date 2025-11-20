using System.Threading.Tasks.Dataflow;
using Backend.Core;
using MessagePack;

namespace Backend.Services;

/// <summary>
/// TPL Dataflow pipeline for metrics multiplexing.
/// Stage 3: Uses 3-way JoinBlock to combine CPU, Network (with collection time), and Disk data.
/// </summary>
public sealed class MultiplexService : IDisposable
{
    private readonly BufferBlock<float[]> _cpuBuffer;
    private readonly BufferBlock<(ulong RxBytes, ulong TxBytes, uint CollectionTimeMs)> _networkBuffer;
    private readonly BufferBlock<(ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops)> _diskBuffer;
    private readonly JoinBlock<float[], (ulong RxBytes, ulong TxBytes, uint CollectionTimeMs), (ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops)> _joinBlock;
    private readonly TransformBlock<Tuple<float[], (ulong, ulong, uint), (ulong, ulong, uint, uint)>, byte[]> _serializeBlock;
    private readonly BroadcastBlock<byte[]> _broadcastBlock;

    public MultiplexService()
    {
        // Stage 3: Separate buffers for CPU, Network (with collection time), and Disk
        _cpuBuffer = new BufferBlock<float[]>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _networkBuffer = new BufferBlock<(ulong, ulong, uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _diskBuffer = new BufferBlock<(ulong, ulong, uint, uint)>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // JoinBlock: Wait for CPU, Network (with collection time), and Disk data before proceeding
        _joinBlock = new JoinBlock<float[], (ulong, ulong, uint), (ulong, ulong, uint, uint)>(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        // Transform block: Serialize combined data to MessagePack
        _serializeBlock = new TransformBlock<Tuple<float[], (ulong, ulong, uint), (ulong, ulong, uint, uint)>, byte[]>(
            combinedData =>
            {
                var (cpuData, networkData, diskData) = combinedData;
                var (rxBytes, txBytes, collectionTimeMs) = networkData;
                var (readBytes, writeBytes, readIops, writeIops) = diskData;
                var snapshot = new MetricsSnapshot
                {
                    TimestampMs = (uint)Environment.TickCount,
                    CpuLoads = cpuData,
                    NetworkRxBytes = rxBytes,
                    NetworkTxBytes = txBytes,
                    DiskReadBytes = readBytes,
                    DiskWriteBytes = writeBytes,
                    DiskReadIops = readIops,
                    DiskWriteIops = writeIops,
                    CollectionTimeMs = collectionTimeMs
                };

                return MessagePackSerializer.Serialize(snapshot);
            },
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 2,
                SingleProducerConstrained = true
            });

        // Broadcast block: Send serialized data to all connected clients
        _broadcastBlock = new BroadcastBlock<byte[]>(bytes => bytes);

        // Link pipeline: CPU/Network/Disk -> Join -> Serialize -> Broadcast
        _cpuBuffer.LinkTo(_joinBlock.Target1, new DataflowLinkOptions { PropagateCompletion = true });
        _networkBuffer.LinkTo(_joinBlock.Target2, new DataflowLinkOptions { PropagateCompletion = true });
        _diskBuffer.LinkTo(_joinBlock.Target3, new DataflowLinkOptions { PropagateCompletion = true });
        _joinBlock.LinkTo(_serializeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _serializeBlock.LinkTo(_broadcastBlock, new DataflowLinkOptions { PropagateCompletion = true });
    }

    /// <summary>
    /// Post CPU metrics to the pipeline.
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostCpuMetrics(float[] cpuData)
    {
        return _cpuBuffer.Post(cpuData);
    }

    /// <summary>
    /// Post Network metrics to the pipeline (includes collection time).
    /// Returns false if the buffer is full (backpressure).
    /// </summary>
    public bool PostNetworkMetrics(ulong rxBytes, ulong txBytes, uint collectionTimeMs)
    {
        return _networkBuffer.Post((rxBytes, txBytes, collectionTimeMs));
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
        _cpuBuffer.Complete();
        _networkBuffer.Complete();
        _diskBuffer.Complete();
        _joinBlock.Complete();
        _serializeBlock.Complete();
        _broadcastBlock.Complete();
    }
}
