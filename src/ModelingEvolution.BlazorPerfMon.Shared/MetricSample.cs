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

    /// <summary>
    /// GPU load percentages (0-100) per GPU.
    /// Null if no GPU metrics are available.
    /// </summary>
    [Key(1)]
    public float[]? GpuLoads { get; init; }

    /// <summary>
    /// CPU load percentages (0-100) per core.
    /// Null if no CPU metrics are available.
    /// </summary>
    [Key(2)]
    public float[]? CpuLoads { get; init; }

    /// <summary>
    /// Network interface metrics (Rx/Tx bytes).
    /// Null if no network metrics are available.
    /// </summary>
    [Key(3)]
    public NetworkMetric[]? NetworkMetrics { get; init; }

    /// <summary>
    /// RAM usage metrics (used and total bytes).
    /// </summary>
    [Key(4)]
    public RamMetric Ram { get; init; }

    /// <summary>
    /// Disk I/O metrics (read/write bytes and IOPS).
    /// Null if no disk metrics are available.
    /// </summary>
    [Key(5)]
    public DiskMetric[]? DiskMetrics { get; init; }

    /// <summary>
    /// Docker container metrics (CPU, memory).
    /// Null if no Docker containers are running or Docker is not available.
    /// </summary>
    [Key(7)]
    public DockerContainerMetric[]? DockerContainers { get; init; }

    /// <summary>
    /// How long it took to collect these metrics (duration in milliseconds).
    /// </summary>
    [Key(6)]
    public uint CollectionDurationMs { get; init; }
}
