using MessagePack;

namespace Frontend.Models;

/// <summary>
/// Describes a single metric source (CPU, GPU, Network interface, Disk, etc.).
/// Used in PerformanceConfigurationSnapshot to inform the client about available metrics.
/// </summary>
[MessagePackObject]
public readonly record struct MetricSource
{
    /// <summary>
    /// Metric type name (e.g., "CPU", "GPU", "Network", "Disk", "RAM").
    /// </summary>
    [Key(0)]
    public string Name { get; init; }

    /// <summary>
    /// Optional identifier for this specific instance (e.g., "eth0", "/dev/sda", "GPU0").
    /// Null for singleton metrics like CPU or RAM.
    /// </summary>
    [Key(1)]
    public string? Identifier { get; init; }

    /// <summary>
    /// Number of data points for this metric (e.g., 16 CPU cores, 2 network interfaces).
    /// For aggregated metrics, this is 1.
    /// </summary>
    [Key(2)]
    public uint Count { get; init; }
}
