using Backend.Core;

namespace Backend.Collectors;

/// <summary>
/// Collects CPU load percentage for each core by reading /proc/stat.
/// Uses delta calculation between reads to determine CPU utilization.
/// Auto-detects number of cores on first read.
/// </summary>
public sealed class CpuCollector : IMetricsCollector<float[]>
{
    private const string ProcStatPath = "/proc/stat";

    private int _coreCount;
    private ulong[]? _prevTotal;
    private ulong[]? _prevIdle;
    private float[]? _loads;
    private bool _isFirstRead = true;

    /// <summary>
    /// Collects current CPU load for all cores.
    /// Returns array of float values (0-100 representing percentage), one per core.
    /// First call returns zeros as baseline is established.
    /// </summary>
    public float[] Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcStatPath);

            // Auto-detect core count on first read
            if (_isFirstRead)
            {
                _coreCount = lines.Count(l => l.StartsWith("cpu") && char.IsDigit(l[3]));
                _prevTotal = new ulong[_coreCount];
                _prevIdle = new ulong[_coreCount];
                _loads = new float[_coreCount];
            }

            for (int core = 0; core < _coreCount; core++)
            {
                // Find line for this core (cpu0, cpu1, etc.)
                var line = lines.FirstOrDefault(l => l.StartsWith($"cpu{core} "));
                if (line == null)
                {
                    _loads[core] = 0f;
                    continue;
                }

                // Parse CPU times
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                {
                    _loads[core] = 0f;
                    continue;
                }

                // CPU line format: cpu0 user nice system idle iowait irq softirq steal guest guest_nice
                // We need: user(1) + nice(2) + system(3) + idle(4) + iowait(5) + irq(6) + softirq(7) + steal(8)
                ulong user = ulong.Parse(parts[1]);
                ulong nice = ulong.Parse(parts[2]);
                ulong system = ulong.Parse(parts[3]);
                ulong idle = ulong.Parse(parts[4]);
                ulong iowait = parts.Length > 5 ? ulong.Parse(parts[5]) : 0;
                ulong irq = parts.Length > 6 ? ulong.Parse(parts[6]) : 0;
                ulong softirq = parts.Length > 7 ? ulong.Parse(parts[7]) : 0;
                ulong steal = parts.Length > 8 ? ulong.Parse(parts[8]) : 0;

                // Total CPU time = all time slices
                ulong total = user + nice + system + idle + iowait + irq + softirq + steal;

                // Idle time includes both idle and iowait
                ulong idleTime = idle + iowait;

                if (_isFirstRead)
                {
                    // First read - establish baseline
                    _loads![core] = 0f;
                }
                else
                {
                    // Calculate delta
                    ulong totalDelta = total - _prevTotal![core];
                    ulong idleDelta = idleTime - _prevIdle![core];

                    if (totalDelta == 0)
                    {
                        _loads![core] = 0f;
                    }
                    else
                    {
                        // CPU load = (total - idle) / total * 100
                        ulong activeDelta = totalDelta - idleDelta;
                        _loads![core] = (float)activeDelta / totalDelta * 100f;

                        // Clamp to 0-100 range
                        if (_loads[core] < 0f) _loads[core] = 0f;
                        if (_loads[core] > 100f) _loads[core] = 100f;
                    }
                }

                // Store current values for next delta calculation
                _prevTotal![core] = total;
                _prevIdle![core] = idleTime;
            }

            _isFirstRead = false;
            return _loads!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CPU stats: {ex.Message}");
            if (_loads != null)
                Array.Fill(_loads, 0f);
            return _loads ?? Array.Empty<float>();
        }
    }
}
