using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Core;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Collects network interface statistics from /proc/net/dev.
/// Returns cumulative bytes (Rx/Tx) since boot for multiple interfaces.
/// Client is responsible for calculating deltas and rates.
/// </summary>
internal sealed class NetworkCollector : IMetricsCollector<NetworkMetric[]>
{
    private const string ProcNetDevPath = "/proc/net/dev";
    private readonly string[] _interfaceNames;

    // Reusable array to avoid ToArray() allocations
    private readonly NetworkMetric[] _metrics;
    private readonly NetworkMetric[] _errorMetrics;

    public NetworkCollector(IOptions<MonitorSettings> settings)
    {
        // Parse comma-separated interface names from config
        var interfaceConfig = settings.Value.NetworkInterface;
        _interfaceNames = interfaceConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (_interfaceNames.Length == 0)
        {
            // Default to eth0 if no config
            _interfaceNames = new[] { "eth0" };
        }

        // Pre-allocate arrays
        _metrics = new NetworkMetric[_interfaceNames.Length];
        _errorMetrics = new NetworkMetric[_interfaceNames.Length];

        // Initialize error metrics once
        for (int i = 0; i < _interfaceNames.Length; i++)
        {
            _errorMetrics[i] = new NetworkMetric
            {
                Identifier = _interfaceNames[i],
                RxBytes = 0,
                TxBytes = 0
            };
        }
    }

    /// <summary>
    /// Collects network statistics for all configured interfaces.
    /// </summary>
    /// <returns>Array of NetworkMetric with cumulative bytes for each interface</returns>
    public NetworkMetric[] Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcNetDevPath);

            for (int i = 0; i < _interfaceNames.Length; i++)
            {
                var interfaceName = _interfaceNames[i];

                // Find the network interface line
                // Format: interface: rxBytes rxPackets ... txBytes txPackets ...
                string? interfaceLine = lines.FirstOrDefault(l => l.Trim().StartsWith(interfaceName + ":"));

                if (interfaceLine == null)
                {
                    // Interface not found, add zero metrics
                    _metrics[i] = new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    };
                    continue;
                }

                // Parse the line
                // Format: interface: rxBytes rxPackets rxErrs rxDrop ... txBytes txPackets txErrs ...
                var parts = interfaceLine.Split(new[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 10)
                {
                    _metrics[i] = new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    };
                    continue;
                }

                // RxBytes is at index 1, TxBytes is at index 9
                if (!ulong.TryParse(parts[1], out ulong rxBytes) ||
                    !ulong.TryParse(parts[9], out ulong txBytes))
                {
                    _metrics[i] = new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    };
                    continue;
                }

                // Return cumulative values - client calculates deltas
                _metrics[i] = new NetworkMetric
                {
                    Identifier = interfaceName,
                    RxBytes = rxBytes,
                    TxBytes = txBytes
                };
            }

            return _metrics;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading network stats: {ex.Message}");
            // Return pre-allocated zero metrics for all interfaces
            return _errorMetrics;
        }
    }
}
