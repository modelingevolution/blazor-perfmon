using ModelingEvolution.BlazorPerfMon.Client.Extensions;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Turns cumulative per-device counters from two consecutive samples into a bytes/second rate.
/// Shared by <see cref="NetworkChart"/> and <see cref="DiskChart"/>, which differ only in the
/// metric array they read and the counter they select.
///
/// Caches the index of the monitored device and re-resolves it only when the array length changes.
/// </summary>
/// <typeparam name="T">The metric element type (network interface or disk device).</typeparam>
internal sealed class MetricRateCalculator<T>
{
    /// <summary>
    /// A gap of more than this many collection intervals between two consecutive samples is a
    /// discontinuity, not a measurement: the client reconnected, the server restarted, or the
    /// view was closed and reopened. The counter delta across such a gap is not a rate anyone
    /// wants plotted, so the rate is reported as zero.
    /// </summary>
    private const int MaxIntervalMultiplier = 4;

    private readonly string _identifier;
    private readonly Func<T, string?> _identifierSelector;
    private readonly float _maxDurationSec;

    private int _index = -1;
    private int _knownCount = -1;

    /// <summary>
    /// Initializes a new instance of the MetricRateCalculator class.
    /// </summary>
    /// <param name="identifier">The device identifier to track (e.g. "eth0", "nvme0n1")</param>
    /// <param name="intervalSec">The server collection interval in seconds</param>
    /// <param name="identifierSelector">Reads the identifier from a metric element</param>
    public MetricRateCalculator(string identifier, float intervalSec, Func<T, string?> identifierSelector)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intervalSec);
        ArgumentNullException.ThrowIfNull(identifierSelector);

        _identifier = identifier;
        _identifierSelector = identifierSelector;
        _maxDurationSec = intervalSec * MaxIntervalMultiplier;
    }

    /// <summary>
    /// Calculates the rate in bytes per second between two consecutive samples.
    /// Returns 0 for the first sample, for a zero-length or discontinuous gap, for an unknown
    /// device, and for a counter that went backwards (reset or wrap).
    /// </summary>
    /// <param name="currentMetrics">Metric array of the current sample</param>
    /// <param name="currentTimestampMs">Timestamp of the current sample</param>
    /// <param name="previousMetrics">Metric array of the previous sample</param>
    /// <param name="previousTimestampMs">Timestamp of the previous sample, 0 when there is none</param>
    /// <param name="valueSelector">Reads the cumulative counter from a metric element</param>
    public float Calculate(
        T[]? currentMetrics,
        uint currentTimestampMs,
        T[]? previousMetrics,
        uint previousTimestampMs,
        Func<T, ulong> valueSelector)
    {
        // No previous sample: nothing to derive a rate from.
        if (previousTimestampMs == 0)
            return 0f;

        if (currentMetrics == null || previousMetrics == null)
            return 0f;

        uint durationMs = currentTimestampMs - previousTimestampMs;
        if (durationMs == 0)
            return 0f;

        float durationSec = durationMs / 1000f;

        // Discontinuity guard - see MaxIntervalMultiplier.
        if (durationSec > _maxDurationSec)
            return 0f;

        // Re-find the device index only if the array length changed.
        if (_index == -1 || currentMetrics.Length != _knownCount)
        {
            _index = currentMetrics.IndexOf(m => _identifierSelector(m) == _identifier);
            _knownCount = currentMetrics.Length;
        }

        if (_index == -1 || _index >= currentMetrics.Length || _index >= previousMetrics.Length)
            return 0f;

        ulong currentValue = valueSelector(currentMetrics[_index]);
        ulong previousValue = valueSelector(previousMetrics[_index]);

        // Counter wrap or reset.
        if (currentValue < previousValue)
            return 0f;

        return (currentValue - previousValue) / durationSec;
    }
}
