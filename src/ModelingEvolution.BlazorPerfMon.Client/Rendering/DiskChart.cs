using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Chart for displaying disk Read/Write metrics.
/// Calculates byte rates from cumulative values provided by server.
/// </summary>
internal sealed class DiskChart : IChart
{
    private readonly string _diskDevice;
    private readonly int _timeWindowMs;

    private readonly SampleDeltaAccessor<float> _readRateAccessor;
    private readonly SampleDeltaAccessor<float> _writeRateAccessor;
    private readonly SampleAccessor<uint> _timestampAccessor;

    private readonly TimeSeriesChart _renderer;
    private readonly DiskTitleFormatter _titleFormatter = new();
    private readonly TimeSeriesF[] _series = new TimeSeriesF[2];

    private readonly MetricRateCalculator<DiskMetric> _rateCalculator;

    private float CalculateRate(in MetricSample current, in MetricSample previous, Func<DiskMetric, ulong> valueSelector) =>
        _rateCalculator.Calculate(current.DiskMetrics, current.CreatedAt, previous.DiskMetrics, previous.CreatedAt, valueSelector);

    /// <summary>
    /// Initializes a new instance of the DiskChart class.
    /// </summary>
    /// <param name="diskDevice">The disk device name to monitor (e.g., "sda", "nvme0n1")</param>
    /// <param name="intervalSec">The server collection interval in seconds, used to detect discontinuities</param>
    /// <param name="timeWindowMs">The time window in milliseconds for historical data display</param>
    /// <param name="timestampAccessor">Accessor for metric timestamps</param>
    public DiskChart(string diskDevice, float intervalSec, int timeWindowMs,
                     SampleAccessor<uint> timestampAccessor)
    {
        _diskDevice = diskDevice;
        _timeWindowMs = timeWindowMs;
        _timestampAccessor = timestampAccessor;
        _rateCalculator = new MetricRateCalculator<DiskMetric>(diskDevice, intervalSec, static m => m.Identifier);

        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _readRateAccessor = new SampleDeltaAccessor<float>(emptyBuffer,
            (in MetricSample current, in MetricSample previous) =>
                CalculateRate(current, previous, m => m.ReadBytes));

        _writeRateAccessor = new SampleDeltaAccessor<float>(emptyBuffer,
            (in MetricSample current, in MetricSample previous) =>
                CalculateRate(current, previous, m => m.WriteBytes));

        _renderer = new TimeSeriesChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _readRateAccessor.UpdateBuffer(buffer);
        _writeRateAccessor.UpdateBuffer(buffer);
        _timestampAccessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Build title with latest values (rates in bytes/sec)
        float readRate = _readRateAccessor.Last();
        float writeRate = _writeRateAccessor.Last();
        Bytes readBytes = (long)readRate;
        Bytes writeBytes = (long)writeRate;

        // Format title with caching - only allocates string if values changed
        var titleData = new DiskTitleData
        {
            DiskDevice = _diskDevice,
            ReadBytes = readBytes,
            WriteBytes = writeBytes
        };
        string title = _titleFormatter.Format(titleData);

        // Update pre-allocated series array - no new allocations
        _series[0] = new TimeSeriesF { Label = "Read", Data = _readRateAccessor, Count = _readRateAccessor.Count, Color = new SKColor(100, 255, 100) };
        _series[1] = new TimeSeriesF { Label = "Write", Data = _writeRateAccessor, Count = _writeRateAccessor.Count, Color = new SKColor(255, 100, 100) };

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
