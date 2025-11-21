using MessagePack;

namespace Frontend.Models;

/// <summary>
/// Configuration snapshot sent once at the beginning of the WebSocket connection.
/// Describes the layout and structure of metrics that will be streamed.
/// </summary>
[MessagePackObject]
public sealed class PerformanceConfigurationSnapshot
{
    /// <summary>
    /// Jagged array representing the grid layout of metric sources.
    /// Each row can have a different number of columns.
    /// Example: [[CPU, GPU, RAM], [Network-eth0, Network-wlan0], [Disk-sda]]
    /// </summary>
    [Key(0)]
    public MetricSource[][]? Layout { get; init; }

    /// <summary>
    /// Collection interval in milliseconds (used for rate calculations).
    /// </summary>
    [Key(1)]
    public uint CollectionIntervalMs { get; init; }

    /// <summary>
    /// Number of data points to keep in the rolling window.
    /// </summary>
    [Key(2)]
    public uint DataPointsToKeep { get; init; }
}
