using Microsoft.Extensions.Options;
using ModelingEvolution.BlazorPerfMon.Server.Core;

namespace ModelingEvolution.BlazorPerfMon.Server.Services;

/// <summary>
/// Builds the PerformanceConfigurationSnapshot from explicit layout configuration.
/// </summary>
public sealed class MetricsConfigurationBuilder
{
    private readonly IOptions<MonitorSettings> _settings;

    public MetricsConfigurationBuilder(IOptions<MonitorSettings> settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Builds the configuration snapshot from explicit layout.
    /// Layout must be provided in configuration.
    /// Parses string[][] from configuration into MetricSource[][].
    /// </summary>
    public PerformanceConfigurationSnapshot BuildConfiguration()
    {
        // Parse string[][] into MetricSource[][]
        var layout = _settings.Value.Layout
            .Select(row => row
                .Select(str => MetricSource.Parse(str, null))
                .ToArray())
            .ToArray();

        return new PerformanceConfigurationSnapshot
        {
            Layout = layout,
            CollectionIntervalMs = (uint)_settings.Value.CollectionIntervalMs,
            DataPointsToKeep = (uint)_settings.Value.DataPointsToKeep
        };
    }
}
