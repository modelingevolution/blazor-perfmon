using MessagePack;

namespace Frontend.Models;

/// <summary>
/// MessagePack-serializable snapshot of all system metrics.
/// MUST match Backend.Core.MetricsSnapshot exactly for deserialization.
/// </summary>
[MessagePackObject]
public sealed class MetricsSnapshot
{
    [Key(0)]
    public uint TimestampMs { get; init; }

    [Key(1)]
    public float GpuLoad { get; init; }

    [Key(2)]
    public float[]? CpuLoads { get; init; }

    [Key(3)]
    public ulong NetworkRxBytes { get; init; }

    [Key(4)]
    public ulong NetworkTxBytes { get; init; }

    [Key(5)]
    public float RamPercent { get; init; }

    [Key(6)]
    public ulong DiskReadBytes { get; init; }

    [Key(7)]
    public ulong DiskWriteBytes { get; init; }

    [Key(8)]
    public uint DiskReadIops { get; init; }

    [Key(9)]
    public uint DiskWriteIops { get; init; }

    [Key(10)]
    public uint CollectionTimeMs { get; init; }
}
