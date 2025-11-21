using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Core;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Collects disk I/O statistics from /proc/diskstats.
/// Returns delta values (read/write bytes and IOPS) since last collection for multiple disks.
/// </summary>
public sealed class DiskCollector : IMetricsCollector<DiskMetric[]>
{
    private const string ProcDiskStatsPath = "/proc/diskstats";
    private const int SectorSize = 512; // Standard sector size in bytes

    private readonly string[] _diskDevices;
    private readonly Dictionary<string, DiskState> _prevStates = new();
    private bool _isFirstRead = true;

    private readonly record struct DiskState(
        ulong SectorsRead,
        ulong SectorsWritten,
        uint ReadsCompleted,
        uint WritesCompleted);

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
    }

    /// <summary>
    /// Collects disk I/O statistics for all configured disk devices.
    /// </summary>
    /// <returns>Array of DiskMetric with delta values for each disk device</returns>
    public DiskMetric[] Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcDiskStatsPath);
            var metrics = new List<DiskMetric>();

            foreach (var diskDevice in _diskDevices)
            {
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
                    metrics.Add(new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    });
                    continue;
                }

                // Parse the line
                var parts = diskLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 14)
                {
                    metrics.Add(new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    });
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
                    metrics.Add(new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    });
                    continue;
                }

                if (_isFirstRead || !_prevStates.ContainsKey(diskDevice))
                {
                    _prevStates[diskDevice] = new DiskState(sectorsRead, sectorsWritten, readsCompleted, writesCompleted);
                    metrics.Add(new DiskMetric
                    {
                        Identifier = diskDevice,
                        ReadBytes = 0,
                        WriteBytes = 0,
                        ReadIops = 0,
                        WriteIops = 0
                    });
                    continue;
                }

                // Calculate deltas
                var prev = _prevStates[diskDevice];

                ulong readBytesDelta = sectorsRead >= prev.SectorsRead
                    ? (sectorsRead - prev.SectorsRead) * SectorSize
                    : 0; // Handle counter reset

                ulong writeBytesDelta = sectorsWritten >= prev.SectorsWritten
                    ? (sectorsWritten - prev.SectorsWritten) * SectorSize
                    : 0; // Handle counter reset

                uint readIopsDelta = readsCompleted >= prev.ReadsCompleted
                    ? readsCompleted - prev.ReadsCompleted
                    : 0;

                uint writeIopsDelta = writesCompleted >= prev.WritesCompleted
                    ? writesCompleted - prev.WritesCompleted
                    : 0;

                _prevStates[diskDevice] = new DiskState(sectorsRead, sectorsWritten, readsCompleted, writesCompleted);

                metrics.Add(new DiskMetric
                {
                    Identifier = diskDevice,
                    ReadBytes = readBytesDelta,
                    WriteBytes = writeBytesDelta,
                    ReadIops = readIopsDelta,
                    WriteIops = writeIopsDelta
                });
            }

            _isFirstRead = false;
            return metrics.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading disk stats: {ex.Message}");
            // Return zero metrics for all disks
            return _diskDevices.Select(device => new DiskMetric
            {
                Identifier = device,
                ReadBytes = 0,
                WriteBytes = 0,
                ReadIops = 0,
                WriteIops = 0
            }).ToArray();
        }
    }
}
