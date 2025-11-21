using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Single sample containing ALL metrics at one point in time (vertical layout).
/// MessagePack-serializable for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct MetricSample
{
    /// <summary>
    /// UTC timestamp when this sample was created (milliseconds since epoch).
    /// </summary>
    [Key(0)]
    public uint CreatedAt { get; init; }

    [Key(1)]
    public float[]? GpuLoads { get; init; }

    [Key(2)]
    public float[]? CpuLoads { get; init; }

    [Key(3)]
    public NetworkMetric[]? NetworkMetrics { get; init; }

    [Key(4)]
    public RamMetric Ram { get; init; }

    [Key(5)]
    public DiskMetric[]? DiskMetrics { get; init; }

    /// <summary>
    /// How long it took to collect these metrics (duration in milliseconds).
    /// </summary>
    [Key(6)]
    public uint CollectionDurationMs { get; init; }
}
