using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Bar chart for displaying per-GPU loads.
/// </summary>
public sealed class GpuBarChart : IChart
{
    private readonly int _gpuCount;
    private readonly SampleAccessor<float>[] _gpuAccessors;
    private readonly SampleAccessor<string>[] _gpuLabelAccessors;
    private readonly BarChart _renderer;

    public GpuBarChart(int gpuCount)
    {
        _gpuCount = gpuCount;
        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _gpuAccessors = new SampleAccessor<float>[gpuCount];
        _gpuLabelAccessors = new SampleAccessor<string>[gpuCount];

        for (int i = 0; i < gpuCount; i++)
        {
            int gpuIndex = i; // Capture for closure
            _gpuAccessors[i] = new SampleAccessor<float>(emptyBuffer, sample =>
                sample.GpuLoads != null && gpuIndex < sample.GpuLoads.Length ? sample.GpuLoads[gpuIndex] : 0f);
            _gpuLabelAccessors[i] = new SampleAccessor<string>(emptyBuffer, sample =>
                gpuCount > 1 ? $"GPU{gpuIndex}" : "GPU");
        }

        _renderer = new BarChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        foreach (var accessor in _gpuAccessors)
            accessor.UpdateBuffer(buffer);
        foreach (var accessor in _gpuLabelAccessors)
            accessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        if (_gpuCount > 0)
        {
            var gpuLoads = _gpuAccessors.Select(accessor => accessor.Last());
            var gpuLabels = _gpuLabelAccessors.Select(accessor => accessor.First());
            _renderer.SetData("GPU", gpuLabels, _gpuCount, gpuLoads, _gpuCount);
        }
        else
        {
            _renderer.SetData("GPU", new[] { "GPU" }, 1, new[] { 0f }, 1);
        }

        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }
}
