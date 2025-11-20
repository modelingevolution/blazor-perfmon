using MessagePack;

namespace Backend.Core;

/// <summary>
/// MessagePack-serializable snapshot of all system metrics.
/// All fields are included for all stages, but only relevant fields are populated per stage.
/// Stage 1: TimestampMs, CpuLoads
/// Stage 2: + NetworkRxBytes, NetworkTxBytes
/// Stage 3: + DiskReadBytes, DiskWriteBytes, DiskReadIops, DiskWriteIops
/// Stage 4: + GpuLoad, RamPercent
/// </summary>
[MessagePackObject]
public sealed class MetricsSnapshot
{
    /// <summary>
    /// Timestamp in milliseconds from Environment.TickCount
    /// </summary>
    [Key(0)]
    public uint TimestampMs { get; init; }

    /// <summary>
    /// GPU load percentage (0-100). Stage 4 only.
    /// </summary>
    [Key(1)]
    public float GpuLoad { get; init; }

    /// <summary>
    /// CPU load percentage per core (0-100). Stage 1+. Array of 8 values for Jetson Orin NX.
    /// </summary>
    [Key(2)]
    public float[]? CpuLoads { get; init; }

    /// <summary>
    /// Network received bytes per second. Stage 2+.
    /// </summary>
    [Key(3)]
    public ulong NetworkRxBytes { get; init; }

    /// <summary>
    /// Network transmitted bytes per second. Stage 2+.
    /// </summary>
    [Key(4)]
    public ulong NetworkTxBytes { get; init; }

    /// <summary>
    /// RAM usage percentage (0-100). Stage 4 only.
    /// </summary>
    [Key(5)]
    public float RamPercent { get; init; }

    /// <summary>
    /// Disk read bytes per second. Stage 3+.
    /// </summary>
    [Key(6)]
    public ulong DiskReadBytes { get; init; }

    /// <summary>
    /// Disk write bytes per second. Stage 3+.
    /// </summary>
    [Key(7)]
    public ulong DiskWriteBytes { get; init; }

    /// <summary>
    /// Disk read IOPS (I/O operations per second). Stage 3+.
    /// </summary>
    [Key(8)]
    public uint DiskReadIops { get; init; }

    /// <summary>
    /// Disk write IOPS (I/O operations per second). Stage 3+.
    /// </summary>
    [Key(9)]
    public uint DiskWriteIops { get; init; }

    /// <summary>
    /// Time taken to collect metrics in milliseconds. Used for monitoring collection performance.
    /// </summary>
    [Key(10)]
    public uint CollectionTimeMs { get; init; }
}
