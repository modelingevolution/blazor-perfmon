namespace Backend.Collectors;

/// <summary>
/// Interface for GPU metrics collection.
/// Implementations provide platform-specific GPU monitoring.
/// </summary>
public interface IGpuCollector
{
    /// <summary>
    /// Collects current GPU utilization percentages for all GPUs.
    /// </summary>
    /// <returns>Array of GPU utilizations (0-100) per GPU. Single element for SMI/NVML, multiple for Tegra.</returns>
    float[] Collect();
}
