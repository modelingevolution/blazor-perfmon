using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Bar chart for displaying per-core CPU loads.
/// </summary>
internal sealed class CpuBarChart : IChart
{
    private readonly int _cpuCount;
    private readonly SampleAccessor<float>[] _cpuAccessors;
    private readonly SampleAccessor<string>[] _cpuLabelAccessors;
    private readonly BarChart _renderer;

    // Reusable arrays to avoid LINQ allocations in hot rendering path
    private readonly float[] _cpuLoads;
    private readonly string[] _cpuLabels;

    /// <summary>
    /// Initializes a new instance of the CpuBarChart class.
    /// </summary>
    /// <param name="cpuCount">The number of CPU cores to display</param>
    public CpuBarChart(int cpuCount)
    {
        _cpuCount = cpuCount;
        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _cpuAccessors = new SampleAccessor<float>[cpuCount];
        _cpuLabelAccessors = new SampleAccessor<string>[cpuCount];
        _cpuLoads = new float[cpuCount];
        _cpuLabels = new string[cpuCount];

        for (int i = 0; i < cpuCount; i++)
        {
            int coreIndex = i; // Capture for closure
            _cpuAccessors[i] = new SampleAccessor<float>(emptyBuffer, sample =>
                sample.CpuLoads != null && coreIndex < sample.CpuLoads.Length ? sample.CpuLoads[coreIndex] : 0f);
            _cpuLabelAccessors[i] = new SampleAccessor<string>(emptyBuffer, sample => $"CPU{coreIndex}");
            _cpuLabels[i] = $"CPU{coreIndex}"; // Initialize labels once
        }

        _renderer = new BarChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        foreach (var accessor in _cpuAccessors)
            accessor.UpdateBuffer(buffer);
        foreach (var accessor in _cpuLabelAccessors)
            accessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Populate arrays without LINQ to avoid allocations in hot path
        for (int i = 0; i < _cpuCount; i++)
        {
            _cpuLoads[i] = _cpuAccessors[i].Last();
        }

        _renderer.SetData("CPU Cores", _cpuLabels, _cpuCount, _cpuLoads, _cpuCount);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
