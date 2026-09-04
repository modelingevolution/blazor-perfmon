namespace ModelingEvolution.BlazorPerfMon.Server.Services;

/// <summary>
/// One complete collection cycle. All metrics of a single tick travel through the pipeline
/// together, so a tick can never be assembled from parts of different cycles.
/// </summary>
/// <param name="CpuLoads">CPU load percentages (0-100) per core.</param>
/// <param name="GpuLoads">GPU load percentages (0-100) per GPU.</param>
/// <param name="Ram">RAM usage of this cycle.</param>
/// <param name="NetworkMetrics">Cumulative network counters per monitored interface.</param>
/// <param name="DiskMetrics">Cumulative disk counters per monitored device.</param>
/// <param name="DockerContainers">Per-container metrics.</param>
/// <param name="TimestampMs">Unix epoch milliseconds truncated to 32 bits, captured before collection started.</param>
/// <param name="CollectionDurationMs">How long collecting this tick took.</param>
internal readonly record struct MetricTick(
    float[] CpuLoads,
    float[] GpuLoads,
    RamMetric Ram,
    NetworkMetric[] NetworkMetrics,
    DiskMetric[] DiskMetrics,
    DockerContainerMetric[] DockerContainers,
    uint TimestampMs,
    uint CollectionDurationMs);
