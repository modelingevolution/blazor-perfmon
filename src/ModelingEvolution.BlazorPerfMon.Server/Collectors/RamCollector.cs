using Backend.Core;

namespace Backend.Collectors;

/// <summary>
/// Collects RAM usage statistics by reading /proc/meminfo.
/// Returns percentage used and absolute byte values (used and total).
/// </summary>
public sealed class RamCollector : IMetricsCollector<RamMetrics>
{
    private const string ProcMeminfoPath = "/proc/meminfo";

    /// <summary>
    /// Collects current RAM usage.
    /// Returns RamMetrics with percentage, used bytes, and total bytes.
    /// </summary>
    public RamMetrics Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcMeminfoPath);

            ulong memTotal = 0;
            ulong memAvailable = 0;
            bool foundTotal = false;
            bool foundAvailable = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    memTotal = ParseMemInfoValue(line);
                    foundTotal = true;
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    memAvailable = ParseMemInfoValue(line);
                    foundAvailable = true;
                }

                if (foundTotal && foundAvailable)
                    break;
            }

            if (memTotal == 0)
            {
                return new RamMetrics
                {
                    UsedPercent = 0f,
                    UsedBytes = 0,
                    TotalBytes = 0
                };
            }

            // Calculate used RAM
            ulong memUsed = memTotal - memAvailable;

            // Calculate percentage
            float usedPercent = (float)memUsed / memTotal * 100f;
            usedPercent = Math.Clamp(usedPercent, 0f, 100f);

            return new RamMetrics
            {
                UsedPercent = usedPercent,
                UsedBytes = memUsed,
                TotalBytes = memTotal
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading RAM stats: {ex.Message}");
            return new RamMetrics
            {
                UsedPercent = 0f,
                UsedBytes = 0,
                TotalBytes = 0
            };
        }
    }

    /// <summary>
    /// Parses a /proc/meminfo line value (in kB) and converts to bytes.
    /// Example input: "MemTotal:       16384000 kB"
    /// </summary>
    private static ulong ParseMemInfoValue(string line)
    {
        var parts = line.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return 0;

        // Value is in format "123456 kB" or just "123456"
        var valuePart = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        if (ulong.TryParse(valuePart, out ulong valueKb))
        {
            // Convert from kB to bytes
            return valueKb * 1024;
        }

        return 0;
    }
}
