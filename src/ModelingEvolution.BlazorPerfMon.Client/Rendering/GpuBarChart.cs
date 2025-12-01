using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Bar chart for displaying per-GPU loads.
/// </summary>
internal sealed class GpuBarChart : IChart
{
    private readonly int _gpuCount;
    private readonly SampleAccessor<float>[] _gpuAccessors;
    private readonly SampleAccessor<string>[] _gpuLabelAccessors;
    private readonly BarChart _renderer;

    // Reusable arrays to avoid LINQ allocations in hot rendering path
    private readonly float[] _gpuLoads;
    private readonly string[] _gpuLabels;

    /// <summary>
    /// Initializes a new instance of the GpuBarChart class.
    /// </summary>
    /// <param name="gpuCount">The number of GPUs to display</param>
    public GpuBarChart(int gpuCount)
    {
        _gpuCount = gpuCount;
        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _gpuAccessors = new SampleAccessor<float>[gpuCount];
        _gpuLabelAccessors = new SampleAccessor<string>[gpuCount];
        _gpuLoads = new float[gpuCount];
        _gpuLabels = new string[gpuCount];

        for (int i = 0; i < gpuCount; i++)
        {
            int gpuIndex = i; // Capture for closure
            _gpuAccessors[i] = new SampleAccessor<float>(emptyBuffer, (in MetricSample sample) =>
                sample.GpuLoads != null && gpuIndex < sample.GpuLoads.Length ? sample.GpuLoads[gpuIndex] : 0f);
            _gpuLabelAccessors[i] = new SampleAccessor<string>(emptyBuffer, (in MetricSample sample) =>
                gpuCount > 1 ? $"GPU{gpuIndex}" : "GPU");
            _gpuLabels[i] = gpuCount > 1 ? $"GPU{gpuIndex}" : "GPU"; // Initialize labels once
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
            // Populate arrays without LINQ to avoid allocations in hot path
            for (int i = 0; i < _gpuCount; i++)
            {
                _gpuLoads[i] = _gpuAccessors[i].Last();
            }
            _renderer.SetData("GPU", _gpuLabels, _gpuCount, _gpuLoads, _gpuCount);
        }
        else
        {
            _renderer.SetData("GPU", new[] { "GPU" }, 1, new[] { 0f }, 1);
        }

        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
