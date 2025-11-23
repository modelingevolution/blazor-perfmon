using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Bar chart for displaying temperature sensor readings.
/// Displays temperatures from thermal zones with color-coded bars.
/// </summary>
internal sealed class TemperatureBarChart : IChart
{
    private readonly int _sensorCount;
    private readonly SampleAccessor<float>[] _temperatureAccessors;
    private readonly SampleAccessor<string>[] _labelAccessors;
    private readonly BarChart _renderer;

    // Reusable arrays to avoid LINQ allocations in hot rendering path
    private readonly float[] _temperatures;
    private readonly string[] _labels;

    /// <summary>
    /// Initializes a new instance of the TemperatureBarChart class.
    /// </summary>
    /// <param name="sensorCount">The number of temperature sensors to display (typically 9 for Tegra)</param>
    public TemperatureBarChart(int sensorCount)
    {
        _sensorCount = sensorCount;
        var emptyBuffer = new ImmutableCircularBuffer<MetricSample>(1);

        _temperatureAccessors = new SampleAccessor<float>[sensorCount];
        _labelAccessors = new SampleAccessor<string>[sensorCount];
        _temperatures = new float[sensorCount];
        _labels = new string[sensorCount];

        for (int i = 0; i < sensorCount; i++)
        {
            int sensorIndex = i; // Capture for closure
            _temperatureAccessors[i] = new SampleAccessor<float>(emptyBuffer, sample =>
                sample.Temperatures != null && sensorIndex < sample.Temperatures.Length ? sample.Temperatures[sensorIndex].TempCelsius : 0f);
            _labelAccessors[i] = new SampleAccessor<string>(emptyBuffer, sample =>
                sample.Temperatures != null && sensorIndex < sample.Temperatures.Length ? sample.Temperatures[sensorIndex].Sensor : $"Sensor{sensorIndex}");
            _labels[i] = $"Sensor{sensorIndex}"; // Default label
        }

        _renderer = new BarChart();
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        foreach (var accessor in _temperatureAccessors)
            accessor.UpdateBuffer(buffer);
        foreach (var accessor in _labelAccessors)
            accessor.UpdateBuffer(buffer);
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        // Populate arrays without LINQ to avoid allocations in hot path
        for (int i = 0; i < _sensorCount; i++)
        {
            _temperatures[i] = _temperatureAccessors[i].Last();
            _labels[i] = _labelAccessors[i].Last();
        }

        _renderer.SetData(
            title: "Temperatures",
            labels: _labels,
            labelCount: _sensorCount,
            values: _temperatures,
            valueCount: _sensorCount,
            minScale: 20f,
            maxScale: 100f,
            units: "°C",
            valueFormat: "{0:F1}",
            colorMapper: ChartStyles.GetTemperaturePaint);
        _renderer.Location = SKPoint.Empty;
        _renderer.Size = size;
        _renderer.Render(canvas);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}
