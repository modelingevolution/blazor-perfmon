using Backend.Core;
using Microsoft.Extensions.Options;

namespace Backend.Collectors;

/// <summary>
/// Collects network interface statistics from /proc/net/dev.
/// Returns delta bytes (Rx/Tx) since last collection.
/// </summary>
public sealed class NetworkCollector : IMetricsCollector<(ulong RxBytes, ulong TxBytes)>
{
    private const string ProcNetDevPath = "/proc/net/dev";
    private readonly string _interfaceName;

    private ulong _prevRxBytes;
    private ulong _prevTxBytes;
    private bool _isFirstRead = true;

    public NetworkCollector(IOptions<MonitorSettings> settings)
    {
        _interfaceName = settings.Value.NetworkInterface;
    }

    /// <summary>
    /// Collects network statistics.
    /// </summary>
    /// <returns>Tuple of (RxBytes delta, TxBytes delta)</returns>
    public (ulong RxBytes, ulong TxBytes) Collect()
    {
        var lines = File.ReadAllLines(ProcNetDevPath);

        // Find the network interface line
        // Format: interface: rxBytes rxPackets ... txBytes txPackets ...
        string? interfaceLine = lines.FirstOrDefault(l => l.Trim().StartsWith(_interfaceName + ":"));

        if (interfaceLine == null)
        {
            // No suitable interface found, return zeros
            return (0, 0);
        }

        // Parse the line
        // Format: interface: rxBytes rxPackets rxErrs rxDrop ... txBytes txPackets txErrs ...
        var parts = interfaceLine.Split(new[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 10)
        {
            return (0, 0);
        }

        // RxBytes is at index 1, TxBytes is at index 9
        if (!ulong.TryParse(parts[1], out ulong currentRx) ||
            !ulong.TryParse(parts[9], out ulong currentTx))
        {
            return (0, 0);
        }

        if (_isFirstRead)
        {
            _prevRxBytes = currentRx;
            _prevTxBytes = currentTx;
            _isFirstRead = false;
            return (0, 0); // First read returns zero delta
        }

        // Calculate deltas
        ulong rxDelta = currentRx >= _prevRxBytes
            ? currentRx - _prevRxBytes
            : 0; // Handle counter reset

        ulong txDelta = currentTx >= _prevTxBytes
            ? currentTx - _prevTxBytes
            : 0; // Handle counter reset

        _prevRxBytes = currentRx;
        _prevTxBytes = currentTx;

        return (rxDelta, txDelta);
    }
}
