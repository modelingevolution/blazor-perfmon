using System.Collections.Immutable;
using System.Threading.Tasks.Dataflow;
using MessagePack;
using Microsoft.Extensions.Logging;
using ModelingEvolution.BlazorPerfMon.Server.Collectors;

namespace ModelingEvolution.BlazorPerfMon.Server.Services;

/// <summary>
/// TPL Dataflow pipeline for metrics multiplexing.
/// One <see cref="MetricTick"/> per collection cycle travels through a single bounded buffer,
/// is serialized to MessagePack and is then fanned out to the currently connected clients.
///
/// Fan-out is explicit rather than a <c>BroadcastBlock</c>: a BroadcastBlock retains its most
/// recent message and offers it to any target linked later, so a client that connected after the
/// engine had stopped received a stale sample carrying old cumulative counters, which the charts
/// turned into a giant first-sample rate spike.
/// See docs/bugs/bug-007-perfmon-network-first-sample-spike.md.
///
/// Tracks connected clients and fires events when the first client connects or the last disconnects.
/// </summary>
internal sealed class MultiplexService : IDisposable
{
    private readonly BufferBlock<MetricTick> _tickBuffer;
    private readonly TransformBlock<MetricTick, byte[]> _serializeBlock;
    private readonly ActionBlock<byte[]> _fanOutBlock;
    private readonly ITemperatureCollector? _temperatureCollector;
    private readonly ILogger<MultiplexService> _logger;

    /// <summary>
    /// Currently linked client targets. Swapped atomically with <see cref="ImmutableInterlocked"/>
    /// so the fan-out reads it without a lock.
    /// </summary>
    private ImmutableList<ITargetBlock<byte[]>> _clientTargets = ImmutableList<ITargetBlock<byte[]>>.Empty;

    private int _clientCount;

    /// <summary>
    /// Fired when the first client connects (transition from 0 to 1 clients).
    /// </summary>
    public event Action? FirstClientConnected;

    /// <summary>
    /// Fired when the last client disconnects (transition from 1 to 0 clients).
    /// </summary>
    public event Action? LastClientDisconnected;

    public MultiplexService(ILogger<MultiplexService> logger, ITemperatureCollector? temperatureCollector = null)
    {
        _logger = logger;
        _temperatureCollector = temperatureCollector;

        // Single bounded buffer: one tick in, one tick out. Nothing to join, nothing to mismatch.
        _tickBuffer = new BufferBlock<MetricTick>(new DataflowBlockOptions
        {
            BoundedCapacity = 2
        });

        _serializeBlock = new TransformBlock<MetricTick, byte[]>(
            Serialize,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 2,
                SingleProducerConstrained = true
            });

        _fanOutBlock = new ActionBlock<byte[]>(
            FanOut,
            new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 2,
                MaxDegreeOfParallelism = 1
            });

        _tickBuffer.LinkTo(_serializeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        _serializeBlock.LinkTo(_fanOutBlock, new DataflowLinkOptions { PropagateCompletion = true });
    }

    /// <summary>
    /// Post one complete collection cycle to the pipeline.
    /// Returns false if the buffer is full (backpressure) and the tick was dropped.
    /// </summary>
    public bool PostMetrics(MetricTick tick)
    {
        if (_tickBuffer.Post(tick))
            return true;

        _logger.LogWarning("Metrics tick {TimestampMs} dropped: pipeline buffer is full", tick.TimestampMs);
        return false;
    }

    private byte[] Serialize(MetricTick tick)
    {
        var sample = new MetricSample
        {
            CreatedAt = tick.TimestampMs,
            GpuLoads = tick.GpuLoads,
            CpuLoads = tick.CpuLoads,
            Ram = tick.Ram,
            DiskMetrics = tick.DiskMetrics,
            NetworkMetrics = tick.NetworkMetrics,
            DockerContainers = tick.DockerContainers,
            CollectionDurationMs = tick.CollectionDurationMs,
            CpuAverage = CalculateAverage(tick.CpuLoads),
            GpuAverage = CalculateAverage(tick.GpuLoads),
            Temperatures = _temperatureCollector?.CollectTemperatures()
        };

        return MessagePackSerializer.Serialize(sample);
    }

    private void FanOut(byte[] data)
    {
        var targets = Volatile.Read(ref _clientTargets);

        for (int i = 0; i < targets.Count; i++)
        {
            if (!targets[i].Post(data))
                _logger.LogWarning("Client target is full, dropping sample for that client");
        }
    }

    /// <summary>
    /// Create a target block that receives metrics from the next produced tick onwards.
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

        ImmutableInterlocked.Update(ref _clientTargets, static (targets, target) => targets.Add(target), (ITargetBlock<byte[]>)actionBlock);

        var newCount = Interlocked.Increment(ref _clientCount);
        _logger.LogInformation("Client target linked, {ClientCount} client(s) connected", newCount);

        if (newCount == 1)
        {
            FirstClientConnected?.Invoke();
        }

        return actionBlock;
    }

    /// <summary>
    /// Unlink a client target from the fan-out.
    /// Call this when a WebSocket client disconnects.
    /// Fires LastClientDisconnected event when transitioning from 1 to 0 clients.
    /// </summary>
    public void UnlinkClientTarget(ITargetBlock<byte[]> target)
    {
        ImmutableInterlocked.Update(ref _clientTargets, static (targets, t) => targets.Remove(t), target);

        target.Complete();

        var newCount = Interlocked.Decrement(ref _clientCount);
        _logger.LogInformation("Client target unlinked, {ClientCount} client(s) connected", newCount);

        if (newCount == 0)
        {
            LastClientDisconnected?.Invoke();
        }
    }

    /// <summary>
    /// Calculates the average of a float array without LINQ to avoid allocations.
    /// </summary>
    private static float CalculateAverage(float[]? values)
    {
        if (values == null || values.Length == 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < values.Length; i++)
            sum += values[i];

        return sum / values.Length;
    }

    public void Dispose()
    {
        _tickBuffer.Complete();
        _serializeBlock.Complete();
        _fanOutBlock.Complete();
    }
}
