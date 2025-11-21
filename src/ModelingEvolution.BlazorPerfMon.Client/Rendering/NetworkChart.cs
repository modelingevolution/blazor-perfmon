using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Chart for displaying network interface Rx/Tx metrics.
/// </summary>
public sealed class NetworkChart : IChart
{
    private readonly string _interfaceName;
    private readonly float _intervalSec;
    private readonly int _timeWindowMs;

    private readonly SampleAccessor<float> _rxAccessor;
    private readonly SampleAccessor<float> _txAccessor;
    private readonly SampleAccessor<uint> _timestampAccessor;

    private readonly TimeSeriesChart _renderer;

    public NetworkChart(string interfaceName, float intervalSec, int timeWindowMs,
                        SampleAccessor<uint> timestampAccessor)
    {
        _interfaceName = interfaceName;
        _intervalSec = intervalSec;
        _timeWindowMs = timeWindowMs;
        _timestampAccessor = timestampAccessor;

        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _rxAccessor = new SampleAccessor<float>(emptyBuffer, sample =>
            sample.NetworkMetrics?.FirstOrDefault(n => n.Identifier == interfaceName)
                .RxBytes / intervalSec ?? 0f);

        _txAccessor = new SampleAccessor<float>(emptyBuffer, sample =>
            sample.NetworkMetrics?.FirstOrDefault(n => n.Identifier == interfaceName)
                .TxBytes / intervalSec ?? 0f);

        _renderer = new TimeSeriesChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _rxAccessor.UpdateBuffer(buffer);
        _txAccessor.UpdateBuffer(buffer);
        _timestampAccessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Build title with latest values
        float rxLatest = _rxAccessor.Last();
        float txLatest = _txAccessor.Last();
        Bytes rxBytes = (long)rxLatest;
        Bytes txBytes = (long)txLatest;
        string title = $"Network {_interfaceName} {rxBytes.FormatFixed()}/s RX, {txBytes.FormatFixed()}/s TX";

        var series = new[]
        {
            new TimeSeriesF { Label = "Rx", Data = _rxAccessor, Count = _rxAccessor.Count, Color = new SKColor(100, 255, 100) },
            new TimeSeriesF { Label = "Tx", Data = _txAccessor, Count = _txAccessor.Count, Color = new SKColor(255, 100, 100) }
        };

        _renderer.Setup(title, series, _timestampAccessor, _timestampAccessor.Count, _timeWindowMs, useDynamicScale: true);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }
}
