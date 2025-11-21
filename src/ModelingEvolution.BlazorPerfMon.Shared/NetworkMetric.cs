using MessagePack;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Network metrics for a single network interface.
/// MessagePack-serializable structure for transmission over WebSocket.
/// </summary>
[MessagePackObject]
public readonly record struct NetworkMetric
{
    /// <summary>
    /// Network interface identifier (e.g., "eth0", "wlan0", "lo").
    /// </summary>
    [Key(0)]
    public string Identifier { get; init; }

    /// <summary>
    /// Bytes received on this interface (cumulative counter).
    /// </summary>
    [Key(1)]
    public ulong RxBytes { get; init; }

    /// <summary>
    /// Bytes transmitted on this interface (cumulative counter).
    /// </summary>
    [Key(2)]
    public ulong TxBytes { get; init; }
}
