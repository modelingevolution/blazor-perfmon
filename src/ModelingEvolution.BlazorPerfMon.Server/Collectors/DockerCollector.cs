using System.Collections.Concurrent;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using ModelingEvolution.BlazorPerfMon.Server.Core;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Collects Docker container metrics using Docker.DotNet client library.
/// Uses a background thread to discover containers every 15 seconds,
/// then sets up continuous monitoring for each container using IProgress.
/// Collect() returns cached data without blocking.
/// </summary>
internal sealed class DockerCollector : IMetricsCollector<DockerContainerMetric[]>, IDisposable
{
    private readonly ILogger<DockerCollector>? _logger;
    private readonly DockerClient? _dockerClient;
    private readonly ConcurrentDictionary<string, DockerContainerMetric> _cachedMetrics = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitoringCancellations = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _discoveryTask;

    // Cached array to avoid ToArray() allocations on every Collect() call
    private volatile DockerContainerMetric[] _cachedArray = Array.Empty<DockerContainerMetric>();
    private volatile bool _arrayNeedsUpdate = false;

    public DockerCollector(ILogger<DockerCollector>? logger = null)
    {
        _logger = logger;

        try
        {
            // Try to create Docker client (Unix socket on Linux, named pipe on Windows)
            var dockerUri = Environment.OSVersion.Platform == PlatformID.Unix
                ? new Uri("unix:///var/run/docker.sock")
                : new Uri("npipe://./pipe/docker_engine");

            _dockerClient = new DockerClientConfiguration(dockerUri).CreateClient();

            // Test connection by pinging Docker
            _dockerClient.System.PingAsync().Wait(TimeSpan.FromSeconds(2));

            _logger?.LogInformation("Docker collector initialized successfully");

            // Start discovery loop
            _discoveryTask = Task.Run(() => DiscoveryLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Docker not available, collector disabled");
        }
    }

    /// <summary>
    /// Discovery loop: checks for new/removed containers every 15 seconds
    /// </summary>
    private async Task DiscoveryLoop(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        // Initial discovery
        await DiscoverContainersAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
                await DiscoverContainersAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in discovery loop");
            }
        }
    }

    private async Task DiscoverContainersAsync(CancellationToken cancellationToken)
    {
        if (_dockerClient == null) return;

        try
        {
            // Get list of running containers only (filter out stopped containers)
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    Limit = 100,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "status", new Dictionary<string, bool> { { "running", true } } }
                    }
                },
                cancellationToken);

            var runningIds = new HashSet<string>();

            // Start monitoring for new containers
            foreach (var container in containers)
            {
                var id = container.ID;
                runningIds.Add(id);

                if (!_monitoringCancellations.ContainsKey(id))
                {
                    var containerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _monitoringCancellations[id] = containerCts;

                    var name = container.Names.FirstOrDefault()?.TrimStart('/') ?? id[..12];
                    _ = Task.Run(() => MonitorContainerAsync(id, name, containerCts.Token), cancellationToken);

                    _logger?.LogInformation("Started monitoring container: {Name} ({Id})", name, id[..12]);
                }
            }

            // Remove metrics for containers that are no longer running
            foreach (var (containerId, cts) in _monitoringCancellations.ToArray())
            {
                if (!runningIds.Contains(containerId))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _monitoringCancellations.TryRemove(containerId, out _);
                    _cachedMetrics.TryRemove(containerId, out _);
                    _arrayNeedsUpdate = true; // Mark array for update

                    _logger?.LogInformation("Stopped monitoring container: {Id}", containerId[..12]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error discovering containers");
        }
    }

    /// <summary>
    /// Monitors a single container continuously using Docker.DotNet streaming stats
    /// </summary>
    private async Task MonitorContainerAsync(string containerId, string name, CancellationToken cancellationToken)
    {
        if (_dockerClient == null) return;

        try
        {
            var progress = new Progress<ContainerStatsResponse>(stats =>
            {
                try
                {
                    // Calculate CPU percentage (normalized to 0-100%)
                    var cpuPercent = CalculateCpuPercent(stats);

                    // Get memory usage
                    var memoryUsage = stats.MemoryStats.Usage;
                    var memoryLimit = stats.MemoryStats.Limit;

                    var metric = new DockerContainerMetric
                    {
                        ContainerId = containerId.Length > 12 ? containerId[..12] : containerId,
                        Name = name,
                        MemoryUsageBytes = memoryUsage,
                        MemoryLimitBytes = memoryLimit,
                        CpuPercent = (float)cpuPercent
                    };

                    _cachedMetrics[containerId] = metric;
                    _arrayNeedsUpdate = true; // Mark array for update
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error parsing stats for container {Name}", name);
                }
            });

            await _dockerClient.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = true },
                progress,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error monitoring container {Name}", name);
        }
    }

    /// <summary>
    /// Calculates CPU percentage normalized to 0-100% (not per-core)
    /// </summary>
    private static double CalculateCpuPercent(ContainerStatsResponse stats)
    {
        var cpuDelta = stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
        var systemDelta = stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;

        if (systemDelta == 0 || cpuDelta == 0)
            return 0.0;

        // Normalize to 0-100% (not per-core percentage)
        // Division by systemDelta already gives us the fraction of total system CPU
        return (double)cpuDelta / systemDelta * 100.0;
    }

    /// <summary>
    /// Returns cached metrics without blocking.
    /// Rebuilds array only when containers are added/removed, not on every call.
    /// </summary>
    public DockerContainerMetric[] Collect()
    {
        // Only rebuild array when containers change, avoiding ToArray() allocation on every call
        if (_arrayNeedsUpdate)
        {
            _cachedArray = _cachedMetrics.Values.ToArray();
            _arrayNeedsUpdate = false;
        }
        return _cachedArray;
    }

    public void Dispose()
    {
        _cts.Cancel();

        // Cancel all container monitoring tasks
        foreach (var (_, cts) in _monitoringCancellations)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _monitoringCancellations.Clear();

        try
        {
            _discoveryTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore
        }

        _cts.Dispose();
        _dockerClient?.Dispose();
    }
}
