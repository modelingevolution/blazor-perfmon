using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Core;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Collects disk I/O statistics from /proc/diskstats.
/// Returns cumulative values (read/write bytes) since boot for multiple disks.
/// Client is responsible for calculating deltas and rates.
/// </summary>
internal sealed class DiskCollector : IMetricsCollector<DiskMetric[]>
{
    private const string ProcDiskStatsPath = "/proc/diskstats";
    private const int SectorSize = 512; // Standard sector size in bytes

    private readonly string[] _diskDevices;

    // Reusable arrays to avoid ToArray() allocations
    private readonly DiskMetric[] _metrics;
    private readonly DiskMetric[] _errorMetrics;

    public DiskCollector(IOptions<MonitorSettings> settings)
    {
        // Parse comma-separated disk device names from config
        var diskConfig = settings.Value.DiskDevice;
        _diskDevices = diskConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (_diskDevices.Length == 0)
        {
            // Default to sda if no config
            _diskDevices = new[] { "sda" };
        }

        // Pre-allocate arrays
        _metrics = new DiskMetric[_diskDevices.Length];
        _errorMetrics = new DiskMetric[_diskDevices.Length];

        // Initialize error metrics once
        for (int i = 0; i < _diskDevices.Length; i++)
        {
            _errorMetrics[i] = new DiskMetric
            {
                Identifier = _diskDevices[i],
                ReadBytes = 0,
                WriteBytes = 0,
                ReadIops = 0,
                WriteIops = 0
            };
        }
    }

    /// <summary>
    /// Collects disk I/O statistics for all configured disk devices.
    /// </summary>
    /// <returns>Array of DiskMetric with cumulative values for each disk device</returns>
    public DiskMetric[] Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcDiskStatsPath);

            for (int i = 0; i < _diskDevices.Length; i++)
            {
                var diskDevice = _diskDevices[i];

                // Find the disk device line
                // Format: major minor name reads_completed ... sectors_read ... writes_completed ... sectors_written ...
                string? diskLine = lines.FirstOrDefault(l =>
                {
                    var parts = l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length >= 3 && parts[2] == diskDevice;
                });

                if (diskLine == null)
                {
                    // Device not found, add zero metrics
                    _metrics[i] = new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    };
                    continue;
                }

                // Parse the line
                var parts = diskLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 14)
                {
                    _metrics[i] = new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    };
                    continue;
                }

                // Field indices (0-based):
                // 3: reads completed
                // 5: sectors read
                // 7: writes completed
                // 9: sectors written
                if (!uint.TryParse(parts[3], out uint readsCompleted) ||
                    !ulong.TryParse(parts[5], out ulong sectorsRead) ||
                    !uint.TryParse(parts[7], out uint writesCompleted) ||
                    !ulong.TryParse(parts[9], out ulong sectorsWritten))
                {
                    _metrics[i] = new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    };
                    continue;
                }

                // Return cumulative values - client calculates deltas
                _metrics[i] = new DiskMetric
                {
                    Identifier = diskDevice,
                    ReadBytes = sectorsRead * SectorSize,
                    WriteBytes = sectorsWritten * SectorSize,
                    ReadIops = readsCompleted,
                    WriteIops = writesCompleted
                };
            }

            return _metrics;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading disk stats: {ex.Message}");
            // Return pre-allocated zero metrics for all disks
            return _errorMetrics;
        }
    }
}
