using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Chart for displaying disk Read/Write metrics.
/// </summary>
internal sealed class DiskChart : IChart
{
    private readonly string _diskDevice;
    private readonly float _intervalSec;
    private readonly int _timeWindowMs;

    private readonly SampleAccessor<float> _readAccessor;
    private readonly SampleAccessor<float> _writeAccessor;
    private readonly SampleAccessor<uint> _timestampAccessor;

    private readonly TimeSeriesChart _renderer;
    private readonly DiskTitleFormatter _titleFormatter = new();
    private readonly TimeSeriesF[] _series = new TimeSeriesF[2];

    /// <summary>
    /// Initializes a new instance of the DiskChart class.
    /// </summary>
    /// <param name="diskDevice">The disk device name to monitor (e.g., "sda", "nvme0n1")</param>
    /// <param name="intervalSec">The collection interval in seconds for rate calculation</param>
    /// <param name="timeWindowMs">The time window in milliseconds for historical data display</param>
    /// <param name="timestampAccessor">Accessor for metric timestamps</param>
    public DiskChart(string diskDevice, float intervalSec, int timeWindowMs,
                     SampleAccessor<uint> timestampAccessor)
    {
        _diskDevice = diskDevice;
        _intervalSec = intervalSec;
        _timeWindowMs = timeWindowMs;
        _timestampAccessor = timestampAccessor;

        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _readAccessor = new SampleAccessor<float>(emptyBuffer, sample =>
            sample.DiskMetrics?.FirstOrDefault(d => d.Identifier == diskDevice)
                .ReadBytes / intervalSec ?? 0f);

        _writeAccessor = new SampleAccessor<float>(emptyBuffer, sample =>
            sample.DiskMetrics?.FirstOrDefault(d => d.Identifier == diskDevice)
                .WriteBytes / intervalSec ?? 0f);

        _renderer = new TimeSeriesChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _readAccessor.UpdateBuffer(buffer);
        _writeAccessor.UpdateBuffer(buffer);
        _timestampAccessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Build title with latest values
        float readLatest = _readAccessor.Last();
        float writeLatest = _writeAccessor.Last();
        Bytes readBytes = (long)readLatest;
        Bytes writeBytes = (long)writeLatest;

        // Format title with caching - only allocates string if values changed
        var titleData = new DiskTitleData
        {
            DiskDevice = _diskDevice,
            ReadBytes = readBytes,
            WriteBytes = writeBytes
        };
        string title = _titleFormatter.Format(titleData);

        // Update pre-allocated series array - no new allocations
        _series[0] = new TimeSeriesF { Label = "Read", Data = _readAccessor, Count = _readAccessor.Count, Color = new SKColor(100, 255, 100) };
        _series[1] = new TimeSeriesF { Label = "Write", Data = _writeAccessor, Count = _writeAccessor.Count, Color = new SKColor(255, 100, 100) };

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
