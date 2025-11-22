using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Docker container metrics.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct DockerContainerMetric
{
    /// <summary>
    /// Container ID (short form, first 12 characters).
    /// </summary>
    [Key(0)]
    public string ContainerId { get; init; }

    /// <summary>
    /// Container name (without leading slash).
    /// </summary>
    [Key(1)]
    public string Name { get; init; }

    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    [Key(2)]
    public ulong MemoryUsageBytes { get; init; }

    /// <summary>
    /// Memory limit in bytes (0 if unlimited).
    /// </summary>
    [Key(3)]
    public ulong MemoryLimitBytes { get; init; }

    /// <summary>
    /// CPU usage percentage (can exceed 100% for multi-core).
    /// This is the raw value from Docker, not normalized.
    /// </summary>
    [Key(4)]
    public float CpuPercent { get; init; }
}
