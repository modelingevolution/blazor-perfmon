using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Core;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Collects disk I/O statistics from /proc/diskstats.
/// Returns delta values (read/write bytes and IOPS) since last collection.
/// </summary>
public sealed class DiskCollector : IMetricsCollector<(ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops)>
{
    private const string ProcDiskStatsPath = "/proc/diskstats";
    private const int SectorSize = 512; // Standard sector size in bytes

    private readonly string _diskDevice;

    private ulong _prevSectorsRead;
    private ulong _prevSectorsWritten;
    private uint _prevReadsCompleted;
    private uint _prevWritesCompleted;
    private bool _isFirstRead = true;

    public DiskCollector(IOptions<MonitorSettings> settings)
    {
        _diskDevice = settings.Value.DiskDevice;
    }

    /// <summary>
    /// Collects disk I/O statistics.
    /// </summary>
    /// <returns>Tuple of (ReadBytes delta, WriteBytes delta, ReadIops delta, WriteIops delta)</returns>
    public (ulong ReadBytes, ulong WriteBytes, uint ReadIops, uint WriteIops) Collect()
    {
        var lines = File.ReadAllLines(ProcDiskStatsPath);

        // Find the disk device line
        // Format: major minor name reads_completed ... sectors_read ... writes_completed ... sectors_written ...
        string? diskLine = lines.FirstOrDefault(l =>
        {
            var parts = l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 && parts[2] == _diskDevice;
        });

        if (diskLine == null)
        {
            // Device not found, return zeros
            return (0, 0, 0, 0);
        }

        // Parse the line
        var parts = diskLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 14)
        {
            return (0, 0, 0, 0);
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
            return (0, 0, 0, 0);
        }

        if (_isFirstRead)
        {
            _prevSectorsRead = sectorsRead;
            _prevSectorsWritten = sectorsWritten;
            _prevReadsCompleted = readsCompleted;
            _prevWritesCompleted = writesCompleted;
            _isFirstRead = false;
            return (0, 0, 0, 0); // First read returns zero delta
        }

        // Calculate deltas
        ulong readBytesDelta = (sectorsRead >= _prevSectorsRead)
            ? (sectorsRead - _prevSectorsRead) * SectorSize
            : 0; // Handle counter reset

        ulong writeBytesDelta = (sectorsWritten >= _prevSectorsWritten)
            ? (sectorsWritten - _prevSectorsWritten) * SectorSize
            : 0; // Handle counter reset

        uint readIopsDelta = (readsCompleted >= _prevReadsCompleted)
            ? readsCompleted - _prevReadsCompleted
            : 0;

        uint writeIopsDelta = (writesCompleted >= _prevWritesCompleted)
            ? writesCompleted - _prevWritesCompleted
            : 0;

        _prevSectorsRead = sectorsRead;
        _prevSectorsWritten = sectorsWritten;
        _prevReadsCompleted = readsCompleted;
        _prevWritesCompleted = writesCompleted;

        return (readBytesDelta, writeBytesDelta, readIopsDelta, writeIopsDelta);
    }
}
