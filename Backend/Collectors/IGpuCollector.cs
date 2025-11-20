namespace Backend.Collectors;

/// <summary>
/// Interface for GPU metrics collection.
/// Implementations provide platform-specific GPU monitoring.
/// </summary>
public interface IGpuCollector
{
    /// <summary>
    /// Collects current GPU utilization percentage.
    /// </summary>
    /// <returns>GPU utilization (0-100)</returns>
    float Collect();
}
