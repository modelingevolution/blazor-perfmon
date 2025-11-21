namespace Backend.Collectors;

/// <summary>
/// Network metrics for a single network interface.
/// </summary>
public readonly record struct NetworkMetric
{
    /// <summary>
    /// Network interface identifier (e.g., "eth0", "wlan0", "lo").
    /// </summary>
    public string Identifier { get; init; }

    /// <summary>
    /// Bytes received on this interface (delta since last collection).
    /// </summary>
    public ulong RxBytes { get; init; }

    /// <summary>
    /// Bytes transmitted on this interface (delta since last collection).
    /// </summary>
    public ulong TxBytes { get; init; }
}
