using System.Diagnostics;
using Backend.Collectors;
using Backend.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Manages the metrics collection loop using PeriodicTimer.
/// Only runs when there are connected WebSocket clients.
/// </summary>
public sealed class PerformanceMonitorEngine : IDisposable
{
    private readonly CpuCollector _cpuCollector;
    private readonly RamCollector _ramCollector;
    private readonly NetworkCollector _networkCollector;
    private readonly DiskCollector _diskCollector;
    private readonly IGpuCollector _gpuCollector;
    private readonly MultiplexService _multiplexService;
    private readonly MonitorSettings _settings;
    private readonly ILogger<PerformanceMonitorEngine> _logger;

    private PeriodicTimer? _metricsTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _collectionTask;
    private readonly object _lock = new();

    public PerformanceMonitorEngine(
        CpuCollector cpuCollector,
        RamCollector ramCollector,
        NetworkCollector networkCollector,
        DiskCollector diskCollector,
        IGpuCollector gpuCollector,
        MultiplexService multiplexService,
        IOptions<MonitorSettings> settings,
        ILogger<PerformanceMonitorEngine> logger)
    {
        _cpuCollector = cpuCollector;
        _ramCollector = ramCollector;
        _networkCollector = networkCollector;
        _diskCollector = diskCollector;
        _gpuCollector = gpuCollector;
        _multiplexService = multiplexService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Start the metrics collection loop.
    /// Safe to call multiple times - will not start if already running.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_metricsTimer != null)
            {
                _logger.LogDebug("Metrics collection already running");
                return;
            }

            _logger.LogInformation("Starting metrics collection (interval: {IntervalMs}ms)", _settings.CollectionIntervalMs);

            _metricsTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_settings.CollectionIntervalMs));
            _cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _cancellationTokenSource.Token;
            var stopwatch = new Stopwatch();

            _collectionTask = Task.Run(async () =>
            {
                while (await _metricsTimer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        // Capture timestamp BEFORE metrics collection starts
                        var timestampMs = (uint)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        stopwatch.Restart();

                        // Collect metrics in parallel with individual timing
                        var cpuSw = Stopwatch.StartNew();
                        var cpuTask = Task.Run(() => { var result = _cpuCollector.Collect(); cpuSw.Stop(); return result; }, cancellationToken);

                        var ramSw = Stopwatch.StartNew();
                        var ramTask = Task.Run(() => { var result = _ramCollector.Collect(); ramSw.Stop(); return result; }, cancellationToken);

                        var gpuSw = Stopwatch.StartNew();
                        var gpuTask = Task.Run(() => { var result = _gpuCollector.Collect(); gpuSw.Stop(); return result; }, cancellationToken);

                        var networkSw = Stopwatch.StartNew();
                        var networkTask = Task.Run(() => { var result = _networkCollector.Collect(); networkSw.Stop(); return result; }, cancellationToken);

                        var diskSw = Stopwatch.StartNew();
                        var diskTask = Task.Run(() => { var result = _diskCollector.Collect(); diskSw.Stop(); return result; }, cancellationToken);

                        await Task.WhenAll(cpuTask, ramTask, gpuTask, networkTask, diskTask);

                        stopwatch.Stop();
                        var collectionTimeMs = (uint)stopwatch.ElapsedMilliseconds;

                        // Log individual collector timings when total exceeds interval
                        if (collectionTimeMs > _settings.CollectionIntervalMs)
                        {
                            _logger.LogWarning("Collection time {CollectionTimeMs}ms exceeds interval {IntervalMs}ms. CPU: {CpuMs}ms, RAM: {RamMs}ms, GPU: {GpuMs}ms, Network: {NetworkMs}ms, Disk: {DiskMs}ms",
                                collectionTimeMs, _settings.CollectionIntervalMs,
                                cpuSw.ElapsedMilliseconds, ramSw.ElapsedMilliseconds, gpuSw.ElapsedMilliseconds,
                                networkSw.ElapsedMilliseconds, diskSw.ElapsedMilliseconds);
                        }

                        // Post to pipeline with timestamp captured before collection
                        var postSuccess = _multiplexService.PostCpuGpuRamMetrics(cpuTask.Result, gpuTask.Result, ramTask.Result, timestampMs)
                                        & _multiplexService.PostNetworkMetrics(networkTask.Result, collectionTimeMs)
                                        & _multiplexService.PostDiskMetrics(diskTask.Result.ReadBytes, diskTask.Result.WriteBytes, diskTask.Result.ReadIops, diskTask.Result.WriteIops);

                        if (!postSuccess)
                            _logger.LogWarning("Backpressure detected: some metrics not posted");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in metrics collection");
                    }
                }
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Stop the metrics collection loop.
    /// Safe to call multiple times - will not fail if already stopped.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (_metricsTimer == null)
            {
                _logger.LogDebug("Metrics collection not running");
                return;
            }

            _logger.LogInformation("Stopping metrics collection");

            _cancellationTokenSource?.Cancel();
            _metricsTimer?.Dispose();

            // Wait for collection task to complete (with timeout)
            if (_collectionTask != null)
            {
                try
                {
                    _collectionTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Expected when task is cancelled
                }
            }

            _cancellationTokenSource?.Dispose();

            _metricsTimer = null;
            _cancellationTokenSource = null;
            _collectionTask = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
