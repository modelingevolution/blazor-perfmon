using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Aggregates temperature metrics from all registered <see cref="ITemperatureSource"/> instances.
/// </summary>
internal sealed class CompositeTemperatureCollector : ITemperatureCollector
{
    private readonly ITemperatureSource[] _sources;

    public CompositeTemperatureCollector(IEnumerable<ITemperatureSource> sources)
    {
        _sources = sources.ToArray();
    }

    public TemperatureMetric[] CollectTemperatures()
    {
        if (_sources.Length == 0)
            return [];

        if (_sources.Length == 1)
            return _sources[0].CollectTemperatures();

        var result = new List<TemperatureMetric>();
        foreach (var source in _sources)
            result.AddRange(source.CollectTemperatures());

        return result.ToArray();
    }
}
