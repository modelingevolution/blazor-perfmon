namespace ModelingEvolution.BlazorPerfMon.Server.Core;

/// <summary>
/// Configuration settings for the monitoring system.
/// </summary>
public sealed class MonitorSettings
{
    /// <summary>
    /// Network interface to monitor (e.g., eth0, eth1, wlan0).
    /// Supports comma-separated values for multiple interfaces.
    /// </summary>
    public string NetworkInterface { get; set; } = "eth0";

    /// <summary>
    /// Disk device to monitor (e.g., sda, nvme0n1, sdb).
    /// Supports comma-separated values for multiple disks.
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

    /// <summary>
    /// GPU collector type: "NvSmi" for desktop GPUs, "NvTegra" for Jetson platforms, "Nvml" for NVML library.
    /// </summary>
    public string GpuCollectorType { get; set; } = "NvSmi";

    /// <summary>
    /// Required explicit layout configuration.
    /// Defines the grid layout of charts to display.
    /// Format: [["CPU/16", "GPU/8", "ComputeLoad/3"], ["Network:eth0/2"], ["Disk:sda/2"]]
    /// Strings are parsed into MetricSource structs by MetricsConfigurationBuilder.
    /// </summary>
    public required string[][] Layout { get; set; }
}
