using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Bar chart for displaying per-core CPU loads.
/// </summary>
public sealed class CpuBarChart : IChart
{
    private readonly int _cpuCount;
    private readonly SampleAccessor<float>[] _cpuAccessors;
    private readonly SampleAccessor<string>[] _cpuLabelAccessors;
    private readonly BarChart _renderer;

    public CpuBarChart(int cpuCount)
    {
        _cpuCount = cpuCount;
        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _cpuAccessors = new SampleAccessor<float>[cpuCount];
        _cpuLabelAccessors = new SampleAccessor<string>[cpuCount];

        for (int i = 0; i < cpuCount; i++)
        {
            int coreIndex = i; // Capture for closure
            _cpuAccessors[i] = new SampleAccessor<float>(emptyBuffer, sample =>
                sample.CpuLoads != null && coreIndex < sample.CpuLoads.Length ? sample.CpuLoads[coreIndex] : 0f);
            _cpuLabelAccessors[i] = new SampleAccessor<string>(emptyBuffer, sample => $"CPU{coreIndex}");
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
        var cpuLoads = _cpuAccessors.Select(accessor => accessor.Last());
        var cpuLabels = _cpuLabelAccessors.Select(accessor => accessor.First());

        _renderer.SetData("CPU Cores", cpuLabels, _cpuCount, cpuLoads, _cpuCount);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }
}
