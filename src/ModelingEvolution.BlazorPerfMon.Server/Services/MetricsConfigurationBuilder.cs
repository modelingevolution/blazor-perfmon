using Backend.Collectors;
using Backend.Core;
using Frontend.Models;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Builds the PerformanceConfigurationSnapshot that describes the metrics layout.
/// </summary>
public sealed class MetricsConfigurationBuilder
{
    private readonly IOptions<MonitorSettings> _settings;
    private readonly CpuCollector _cpuCollector;
    private readonly NetworkCollector _networkCollector;
    private readonly IGpuCollector _gpuCollector;

    public MetricsConfigurationBuilder(
        IOptions<MonitorSettings> settings,
        CpuCollector cpuCollector,
        NetworkCollector networkCollector,
        IGpuCollector gpuCollector)
    {
        _settings = settings;
        _cpuCollector = cpuCollector;
        _networkCollector = networkCollector;
        _gpuCollector = gpuCollector;
    }

    /// <summary>
    /// Builds the configuration snapshot by detecting available metrics.
    /// </summary>
    public PerformanceConfigurationSnapshot BuildConfiguration()
    {
        // Collect one sample from each collector to detect capabilities
        var cpuLoads = _cpuCollector.Collect();
        var gpuLoads = _gpuCollector.Collect();
        var networkMetrics = _networkCollector.Collect();

        // Build layout:
        // Row 1: [CPU (bar chart), GPU (bar chart), CPU/GPU/RAM Load (time series)]
        // Row 2: [Network interfaces], [Disk I/O]

        var layout = new List<MetricSource[]>();

        // Row 1: CPU cores, GPU cores, and aggregated time series
        var row1 = new List<MetricSource>();

        // CPU cores (for bar chart)
        if (cpuLoads != null && cpuLoads.Length > 0)
        {
            row1.Add(new MetricSource
            {
                Name = "CPU",
                Identifier = null,
                Count = (uint)cpuLoads.Length
            });
        }

        // GPU cores (for bar chart)
        if (gpuLoads != null && gpuLoads.Length > 0)
        {
            row1.Add(new MetricSource
            {
                Name = "GPU",
                Identifier = null,
                Count = (uint)gpuLoads.Length
            });
        }

        // CPU/GPU/RAM aggregated time series
        row1.Add(new MetricSource
        {
            Name = "Load",
            Identifier = "CPU/GPU/RAM",
            Count = 3 // CPU avg, GPU avg, RAM %
        });

        layout.Add(row1.ToArray());

        // Row 2: Network and Disk
        var row2 = new List<MetricSource>();

        // Network interfaces
        if (networkMetrics != null && networkMetrics.Length > 0)
        {
            foreach (var networkMetric in networkMetrics)
            {
                row2.Add(new MetricSource
                {
                    Name = "Network",
                    Identifier = networkMetric.Identifier,
                    Count = 2 // Rx + Tx
                });
            }
        }

        // Disk I/O (single for now, could be extended for multiple disks)
        row2.Add(new MetricSource
        {
            Name = "Disk",
            Identifier = null,
            Count = 2 // Read + Write
        });

        layout.Add(row2.ToArray());

        return new PerformanceConfigurationSnapshot
        {
            Layout = layout.ToArray(),
            CollectionIntervalMs = (uint)_settings.Value.CollectionIntervalMs,
            DataPointsToKeep = (uint)_settings.Value.DataPointsToKeep
        };
    }
}
