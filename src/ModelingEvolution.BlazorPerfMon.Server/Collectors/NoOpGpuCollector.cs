using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// No-op GPU collector that returns zero/empty values when GPU monitoring is disabled.
/// Used when GpuCollectorType is set to "none" or when no GPU is available.
/// </summary>
internal sealed class NoOpGpuCollector : IGpuCollector, ITemperatureCollector
{
    /// <summary>
    /// Returns zero GPU utilization (GPU monitoring disabled).
    /// </summary>
    /// <returns>Single-element array with 0.0f</returns>
    public float[] Collect()
    {
        return new float[] { 0f };
    }

    /// <summary>
    /// Returns empty temperature array (GPU monitoring disabled).
    /// </summary>
    /// <returns>Empty array</returns>
    public TemperatureMetric[] CollectTemperatures()
    {
        return Array.Empty<TemperatureMetric>();
    }
}
