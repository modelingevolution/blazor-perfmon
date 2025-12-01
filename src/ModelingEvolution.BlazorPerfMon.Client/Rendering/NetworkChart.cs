using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Chart for displaying network interface Rx/Tx metrics.
/// Calculates byte rates from cumulative values provided by server.
/// </summary>
internal sealed class NetworkChart : IChart
{
    private readonly string _interfaceName;
    private readonly int _timeWindowMs;

    private readonly SampleDeltaAccessor<float> _rxRateAccessor;
    private readonly SampleDeltaAccessor<float> _txRateAccessor;
    private readonly SampleAccessor<uint> _timestampAccessor;

    private readonly TimeSeriesChart _renderer;
    private readonly NetworkTitleFormatter _titleFormatter = new();
    private readonly TimeSeriesF[] _series = new TimeSeriesF[2];

    // Cached interface index to avoid IndexOf on every sample
    private int _interfaceIndex = -1;
    private int _interfaceCount = 0;

    private float CalculateRate(in MetricSample current, in MetricSample previous, Func<NetworkMetric, ulong> valueSelector)
    {
        // Handle first sample (no previous)
        if (previous.CreatedAt == 0)
            return 0f;

        uint durationMs = current.CreatedAt - previous.CreatedAt;
        if (durationMs == 0)
            return 0f;

        var currentMetrics = current.NetworkMetrics;
        var previousMetrics = previous.NetworkMetrics;

        if (currentMetrics == null || previousMetrics == null)
            return 0f;

        // Re-find interface index only if array length changed
        if (_interfaceIndex == -1 || currentMetrics.Length != _interfaceCount)
        {
            _interfaceIndex = currentMetrics.IndexOf(n => n.Identifier == _interfaceName);
            _interfaceCount = currentMetrics.Length;
        }

        if (_interfaceIndex == -1 || _interfaceIndex >= currentMetrics.Length || _interfaceIndex >= previousMetrics.Length)
            return 0f;

        // Calculate delta bytes and rate using cached index
        ulong currentValue = valueSelector(currentMetrics[_interfaceIndex]);
        ulong previousValue = valueSelector(previousMetrics[_interfaceIndex]);

        // Handle counter wrap/reset
        if (currentValue < previousValue)
            return 0f;

        ulong deltaBytes = currentValue - previousValue;
        float durationSec = durationMs / 1000f;

        return deltaBytes / durationSec;
    }
    /// <summary>
    /// Initializes a new instance of the NetworkChart class.
    /// </summary>
    /// <param name="interfaceName">The network interface name to monitor (e.g., "eth0", "wlan0")</param>
    /// <param name="intervalSec">The collection interval in seconds (unused - kept for API compatibility)</param>
    /// <param name="timeWindowMs">The time window in milliseconds for historical data display</param>
    /// <param name="timestampAccessor">Accessor for metric timestamps</param>
    public NetworkChart(string interfaceName, float intervalSec, int timeWindowMs,
                        SampleAccessor<uint> timestampAccessor)
    {
        _interfaceName = interfaceName;
        _timeWindowMs = timeWindowMs;
        _timestampAccessor = timestampAccessor;

        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _rxRateAccessor = new SampleDeltaAccessor<float>(emptyBuffer,
            (in MetricSample current, in MetricSample previous) =>
                CalculateRate(current, previous, m => m.RxBytes));

        _txRateAccessor = new SampleDeltaAccessor<float>(emptyBuffer,
            (in MetricSample current, in MetricSample previous) =>
                CalculateRate(current, previous, m => m.TxBytes));

        _renderer = new TimeSeriesChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _rxRateAccessor.UpdateBuffer(buffer);
        _txRateAccessor.UpdateBuffer(buffer);
        _timestampAccessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Build title with latest values (rates in bytes/sec)
        float rxRate = _rxRateAccessor.Last();
        float txRate = _txRateAccessor.Last();
        Bytes rxBytes = (long)rxRate;
        Bytes txBytes = (long)txRate;

        // Format title with caching - only allocates string if values changed
        var titleData = new NetworkTitleData
        {
            InterfaceName = _interfaceName,
            RxBytes = rxBytes,
            TxBytes = txBytes
        };
        string title = _titleFormatter.Format(titleData);

        // Update pre-allocated series array - no new allocations
        _series[0] = new TimeSeriesF { Label = "Rx", Data = _rxRateAccessor, Count = _rxRateAccessor.Count, Color = new SKColor(100, 255, 100) };
        _series[1] = new TimeSeriesF { Label = "Tx", Data = _txRateAccessor, Count = _txRateAccessor.Count, Color = new SKColor(255, 100, 100) };

        _renderer.Setup(title, _series, _timestampAccessor, _timestampAccessor.Count, _timeWindowMs, useDynamicScale: true);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
