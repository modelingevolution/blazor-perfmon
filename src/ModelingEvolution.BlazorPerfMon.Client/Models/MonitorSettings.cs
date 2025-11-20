namespace Frontend.Models;

/// <summary>
/// Configuration settings for the monitoring system (frontend).
/// </summary>
public sealed class MonitorSettings
{
    /// <summary>
    /// Network interface to monitor (e.g., eth0, eth1, wlan0).
    /// </summary>
    public string NetworkInterface { get; set; } = "eth0";

    /// <summary>
    /// Collection interval in milliseconds (default: 500ms for 2Hz).
    /// </summary>
    public int CollectionIntervalMs { get; set; } = 500;

    /// <summary>
    /// Number of data points to keep in rolling window (default: 120 for 60 seconds at 2Hz).
    /// </summary>
    public int DataPointsToKeep { get; set; } = 120;
}
