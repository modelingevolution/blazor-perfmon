using MessagePack;

namespace Frontend.Models;

/// <summary>
/// Disk I/O metrics.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct DiskMetric
{
    /// <summary>
    /// Bytes read from disk (delta since last sample).
    /// </summary>
    [Key(0)]
    public ulong ReadBytes { get; init; }

    /// <summary>
    /// Bytes written to disk (delta since last sample).
    /// </summary>
    [Key(1)]
    public ulong WriteBytes { get; init; }

    /// <summary>
    /// Read I/O operations per second.
    /// </summary>
    [Key(2)]
    public uint ReadIops { get; init; }

    /// <summary>
    /// Write I/O operations per second.
    /// </summary>
    [Key(3)]
    public uint WriteIops { get; init; }
}
