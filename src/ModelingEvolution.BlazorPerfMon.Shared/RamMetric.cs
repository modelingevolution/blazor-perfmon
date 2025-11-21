using MessagePack;

namespace Frontend.Models;

/// <summary>
/// RAM memory metrics.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct RamMetric
{
    /// <summary>
    /// Bytes of RAM currently in use.
    /// </summary>
    [Key(0)]
    public ulong UsedBytes { get; init; }

    /// <summary>
    /// Total bytes of RAM available.
    /// </summary>
    [Key(1)]
    public ulong TotalBytes { get; init; }
}
