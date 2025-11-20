namespace Backend.Core;

/// <summary>
/// Interface for metrics collectors.
/// Collectors are responsible for reading system metrics from various sources.
/// </summary>
/// <typeparam name="T">The type of metrics data returned</typeparam>
public interface IMetricsCollector<out T>
{
    /// <summary>
    /// Collect the current metric value(s).
    /// This method should be fast and non-blocking.
    /// </summary>
    /// <returns>The collected metric data</returns>
    T Collect();
}
