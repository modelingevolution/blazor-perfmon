using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Disk I/O metrics for a single disk device.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct DiskMetric
{
    /// <summary>
    /// Disk device identifier (e.g., "sda", "nvme0n1", "sdb").
    /// </summary>
    [Key(0)]
    public string? Identifier { get; init; }

    /// <summary>
    /// Bytes read from disk (delta since last sample).
    /// </summary>
    [Key(1)]
    public ulong ReadBytes { get; init; }

    /// <summary>
    /// Bytes written to disk (delta since last sample).
    /// </summary>
    [Key(2)]
    public ulong WriteBytes { get; init; }

    /// <summary>
    /// Read I/O operations per second.
    /// </summary>
    [Key(3)]
    public uint ReadIops { get; init; }

    /// <summary>
    /// Write I/O operations per second.
    /// </summary>
    [Key(4)]
    public uint WriteIops { get; init; }
}
