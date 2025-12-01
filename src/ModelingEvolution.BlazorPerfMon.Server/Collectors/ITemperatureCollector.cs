using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Interface for temperature metrics collection.
/// Implementations provide platform-specific temperature monitoring for various sensors
/// (GPU, CPU, chipset, etc.).
/// </summary>
public interface ITemperatureCollector
{
    /// <summary>
    /// Collect temperature metrics from all available sensors.
    /// </summary>
    /// <returns>Array of temperature metrics</returns>
    TemperatureMetric[] CollectTemperatures();
}
