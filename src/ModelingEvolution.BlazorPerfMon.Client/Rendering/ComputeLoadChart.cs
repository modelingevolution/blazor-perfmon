using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Time series chart for displaying compute load (CPU Avg, GPU, RAM).
/// </summary>
internal sealed class ComputeLoadChart : IChart
{
    private readonly int _timeWindowMs;
    private readonly SampleAccessor<float> _cpuAvgAccessor;
    private readonly SampleAccessor<float> _gpuAvgAccessor;
    private readonly SampleAccessor<float> _ramPercentAccessor;
    private readonly SampleAccessor<uint> _timestampAccessor;
    private readonly TimeSeriesChart _renderer;
    private ImmutableCircularBuffer<MetricSample> _buffer = new ImmutableCircularBuffer<MetricSample>(1);

    /// <summary>
    /// Initializes a new instance of the ComputeLoadChart class.
    /// </summary>
    /// <param name="timeWindowMs">The time window in milliseconds for historical data display</param>
    /// <param name="timestampAccessor">Accessor for metric timestamps</param>
    public ComputeLoadChart(int timeWindowMs, SampleAccessor<uint> timestampAccessor)
    {
        _timeWindowMs = timeWindowMs;
        _timestampAccessor = timestampAccessor;

        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _cpuAvgAccessor = new SampleAccessor<float>(emptyBuffer, sample => sample.CpuAverage);

        _gpuAvgAccessor = new SampleAccessor<float>(emptyBuffer, sample => sample.GpuAverage);

        _ramPercentAccessor = new SampleAccessor<float>(emptyBuffer, sample =>
            sample.Ram.TotalBytes > 0 ? (float)sample.Ram.UsedBytes / sample.Ram.TotalBytes * 100f : 0f);

        _renderer = new TimeSeriesChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _buffer = buffer;
        _cpuAvgAccessor.UpdateBuffer(buffer);
        _gpuAvgAccessor.UpdateBuffer(buffer);
        _ramPercentAccessor.UpdateBuffer(buffer);
        _timestampAccessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Get latest values for title
        if (_buffer.Count == 0)
            return;

        var latestSample = _buffer.Last();
        float cpuAvgLatest = _cpuAvgAccessor.Last();
        float gpuAvgLatest = _gpuAvgAccessor.Last();
        ulong ramUsedBytes = latestSample.Ram.UsedBytes;
        ulong ramTotalBytes = latestSample.Ram.TotalBytes;
        Bytes ramUsed = (long)ramUsedBytes;
        Bytes ramTotal = (long)ramTotalBytes;
        string title = $"Compute Load {cpuAvgLatest:F1}% CPU, {gpuAvgLatest:F1}% GPU, {ramUsed}/{ramTotal} RAM";

        var series = new[]
        {
            new TimeSeriesF { Label = "CPU Avg", Data = _cpuAvgAccessor, Count = _cpuAvgAccessor.Count, Color = new SKColor(100, 255, 100) },
            new TimeSeriesF { Label = "GPU", Data = _gpuAvgAccessor, Count = _gpuAvgAccessor.Count, Color = new SKColor(255, 200, 100) },
            new TimeSeriesF { Label = "RAM", Data = _ramPercentAccessor, Count = _ramPercentAccessor.Count, Color = new SKColor(100, 200, 255) }
        };

        _renderer.Setup(title, series, _timestampAccessor, _timestampAccessor.Count, _timeWindowMs, useDynamicScale: false);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
