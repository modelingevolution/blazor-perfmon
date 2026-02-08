using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Individual temperature source. Implement this interface to provide temperature metrics
/// from a specific source (GPU, thermal zones, custom sensors, etc.).
/// All registered sources are automatically aggregated by <see cref="CompositeTemperatureCollector"/>.
/// </summary>
public interface ITemperatureSource
{
    /// <summary>
    /// Collect temperature metrics from this source.
    /// </summary>
    TemperatureMetric[] CollectTemperatures();
}
