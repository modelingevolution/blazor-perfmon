namespace Backend.Core;

/// <summary>
/// Configuration settings for the monitoring system.
/// </summary>
public sealed class MonitorSettings
{
    /// <summary>
    /// Network interface to monitor (e.g., eth0, eth1, wlan0).
    /// </summary>
    public string NetworkInterface { get; set; } = "eth0";

    /// <summary>
    /// Disk device to monitor (e.g., sda, nvme0n1, sdb).
    /// </summary>
    public string DiskDevice { get; set; } = "sda";

    /// <summary>
    /// Collection interval in milliseconds (default: 500ms for 2Hz).
    /// </summary>
    public int CollectionIntervalMs { get; set; } = 500;

    /// <summary>
    /// Number of data points to keep in rolling window (default: 120 for 60 seconds at 2Hz).
    /// </summary>
    public int DataPointsToKeep { get; set; } = 120;
}
