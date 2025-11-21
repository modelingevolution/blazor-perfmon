using Backend.Core;
using Microsoft.Extensions.Options;

namespace Backend.Collectors;

/// <summary>
/// Collects network interface statistics from /proc/net/dev.
/// Returns delta bytes (Rx/Tx) since last collection for multiple interfaces.
/// </summary>
public sealed class NetworkCollector : IMetricsCollector<NetworkMetric[]>
{
    private const string ProcNetDevPath = "/proc/net/dev";
    private readonly string[] _interfaceNames;

    private readonly Dictionary<string, (ulong RxBytes, ulong TxBytes)> _prevValues = new();
    private bool _isFirstRead = true;

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
    }

    /// <summary>
    /// Collects network statistics for all configured interfaces.
    /// </summary>
    /// <returns>Array of NetworkMetric with delta bytes for each interface</returns>
    public NetworkMetric[] Collect()
    {
        try
        {
            var lines = File.ReadAllLines(ProcNetDevPath);
            var metrics = new List<NetworkMetric>();

            foreach (var interfaceName in _interfaceNames)
            {
                // Find the network interface line
                // Format: interface: rxBytes rxPackets ... txBytes txPackets ...
                string? interfaceLine = lines.FirstOrDefault(l => l.Trim().StartsWith(interfaceName + ":"));

                if (interfaceLine == null)
                {
                    // Interface not found, add zero metrics
                    metrics.Add(new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    });
                    continue;
                }

                // Parse the line
                // Format: interface: rxBytes rxPackets rxErrs rxDrop ... txBytes txPackets txErrs ...
                var parts = interfaceLine.Split(new[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 10)
                {
                    metrics.Add(new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    });
                    continue;
                }

                // RxBytes is at index 1, TxBytes is at index 9
                if (!ulong.TryParse(parts[1], out ulong currentRx) ||
                    !ulong.TryParse(parts[9], out ulong currentTx))
                {
                    metrics.Add(new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    });
                    continue;
                }

                if (_isFirstRead || !_prevValues.ContainsKey(interfaceName))
                {
                    _prevValues[interfaceName] = (currentRx, currentTx);
                    metrics.Add(new NetworkMetric
                    {
                        Identifier = interfaceName,
                        RxBytes = 0,
                        TxBytes = 0
                    });
                    continue;
                }

                // Calculate deltas
                var prev = _prevValues[interfaceName];
                ulong rxDelta = currentRx >= prev.RxBytes
                    ? currentRx - prev.RxBytes
                    : 0; // Handle counter reset

                ulong txDelta = currentTx >= prev.TxBytes
                    ? currentTx - prev.TxBytes
                    : 0; // Handle counter reset

                _prevValues[interfaceName] = (currentRx, currentTx);

                metrics.Add(new NetworkMetric
                {
                    Identifier = interfaceName,
                    RxBytes = rxDelta,
                    TxBytes = txDelta
                });
            }

            _isFirstRead = false;
            return metrics.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading network stats: {ex.Message}");
            // Return zero metrics for all interfaces
            return _interfaceNames.Select(name => new NetworkMetric
            {
                Identifier = name,
                RxBytes = 0,
                TxBytes = 0
            }).ToArray();
        }
    }
}
