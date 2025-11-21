using ManagedCuda.Nvml;
using Backend.Core;
using Microsoft.Extensions.Options;

namespace Backend.Collectors;

/// <summary>
/// GPU collector for desktop NVIDIA GPUs using NVML library.
/// Much faster than nvidia-smi (~1-2ms vs 300-900ms).
/// Supports Turing, Ampere, Ada Lovelace architectures.
/// Uses timeout (1/3 of tick interval) to prevent slow idle GPU queries.
/// </summary>
public sealed class NvmlGpuCollector : IGpuCollector, IDisposable
{
    private readonly ILogger<NvmlGpuCollector> _logger;
    private readonly int _timeoutMs;
    private readonly object _lock = new();
    private bool _initialized;
    private bool _disposed;
    private nvmlDevice _device;
    private float _lastValue = 0f;

    public NvmlGpuCollector(ILogger<NvmlGpuCollector> logger, IOptions<MonitorSettings> settings)
    {
        _logger = logger;
        _timeoutMs = settings.Value.CollectionIntervalMs / 3;
        _logger.LogInformation("NVML GPU collector timeout set to {TimeoutMs}ms (1/3 of {IntervalMs}ms tick)",
            _timeoutMs, settings.Value.CollectionIntervalMs);
        Initialize();
    }

    private void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                // Initialize NVML
                var result = NvmlNativeMethods.nvmlInit();
                if (result != nvmlReturn.Success)
                {
                    _logger.LogWarning("Failed to initialize NVML: {Result}", result);
                    return;
                }

                // Get first GPU device
                result = NvmlNativeMethods.nvmlDeviceGetHandleByIndex(0, ref _device);
                if (result != nvmlReturn.Success)
                {
                    _logger.LogWarning("Failed to get GPU device handle: {Result}", result);
                    NvmlNativeMethods.nvmlShutdown();
                    return;
                }

                _initialized = true;
                _logger.LogInformation("NVML GPU collector initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing NVML GPU collector");
            }
        }
    }

    /// <summary>
    /// Collects GPU utilization using NVML library with timeout protection.
    /// Returns cached value if query takes longer than timeout (GPU in deep sleep).
    /// Typically takes 0-2ms when GPU is active, 300-400ms when idle.
    /// </summary>
    /// <returns>Single-element array with GPU utilization percentage (0-100)</returns>
    public float[] Collect()
    {
        if (!_initialized || _disposed)
        {
            return new float[] { 0f };
        }

        try
        {
            using var cts = new CancellationTokenSource(_timeoutMs);
            var task = Task.Run(() => CollectWithoutTimeout(), cts.Token);

            if (task.Wait(_timeoutMs))
            {
                // Completed within timeout
                _lastValue = task.Result;
                return new float[] { _lastValue };
            }
            else
            {
                // Timeout - GPU likely in deep sleep, return cached value
                _logger.LogDebug("GPU query timeout ({TimeoutMs}ms), returning cached value: {Value}%",
                    _timeoutMs, _lastValue);
                return new float[] { _lastValue };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting GPU metrics via NVML");
            return new float[] { _lastValue };
        }
    }

    private float CollectWithoutTimeout()
    {
        nvmlUtilization utilization = new nvmlUtilization();
        var result = NvmlNativeMethods.nvmlDeviceGetUtilizationRates(_device, ref utilization);

        // NVML sometimes returns "Unknown" in virtual environments (WSL) but data is still valid
        // Only reject clearly failed calls
        if (result != nvmlReturn.Success && result != nvmlReturn.Unknown)
        {
            _logger.LogWarning("Failed to get GPU utilization: {Result}", result);
            return _lastValue;
        }

        // utilization.gpu is already a percentage (0-100)
        return Math.Clamp(utilization.gpu, 0f, 100f);
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_disposed) return;

            if (_initialized)
            {
                try
                {
                    NvmlNativeMethods.nvmlShutdown();
                    _logger.LogInformation("NVML GPU collector shutdown successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error shutting down NVML");
                }
            }

            _disposed = true;
        }
    }
}
