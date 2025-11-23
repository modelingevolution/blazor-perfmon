using System.Diagnostics;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// GPU collector for NVIDIA Jetson platforms using tegrastats.
/// Supports Jetson Orin NX, AGX Orin, and other Tegra-based devices.
/// Parses complete tegrastats output including RAM, CPU, temperatures, and power metrics.
/// </summary>
internal sealed class NvTegraGpuCollector : IGpuCollector
{
    private readonly ILogger<NvTegraGpuCollector> _logger;
    private Process? _tegrastatsProcess;
    private TegraStatsLine? _latestStats;

    public NvTegraGpuCollector(ILogger<NvTegraGpuCollector> logger)
    {
        _logger = logger;
        StartTegrastats();
    }

    /// <summary>
    /// Gets the latest complete tegrastats data.
    /// Useful for accessing additional metrics beyond GPU utilization.
    /// </summary>
    public TegraStatsLine? LatestStats => _latestStats;

    /// <summary>
    /// Starts tegrastats process in background to continuously monitor all system metrics.
    /// </summary>
    private void StartTegrastats()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "tegrastats",
                Arguments = "--interval 500", // 500ms interval
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _tegrastatsProcess = Process.Start(startInfo);
            if (_tegrastatsProcess == null)
            {
                _logger.LogWarning("Failed to start tegrastats process");
                return;
            }

            // Use asynchronous event-based reading (non-blocking)
            _tegrastatsProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ParseTegrastatsLine(e.Data);
                }
            };
            _tegrastatsProcess.BeginOutputReadLine();

            _logger.LogInformation("tegrastats monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tegrastats");
        }
    }

    /// <summary>
    /// Parses complete tegrastats output line including RAM, SWAP, CPU, GPU, temperatures, and power.
    /// Example: "11-23-2025 22:47:56 RAM 1729/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) CPU [1%@1420,2%@1420,...] GR3D_FREQ 0% cv0@43.562C ... VDD_IN 6120mW/6120mW ..."
    /// </summary>
    private void ParseTegrastatsLine(string line)
    {
        try
        {
            if (TegraStatsLine.TryParse(line, null, out var stats))
            {
                _latestStats = stats;
            }
            else
            {
                _logger.LogWarning("Failed to parse tegrastats line: {Line}", line);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing tegrastats line: {Line}", line);
        }
    }

    /// <summary>
    /// Returns the latest GPU utilization from tegrastats.
    /// Currently returns single-element array for GR3D_FREQ.
    /// </summary>
    public float[] Collect()
    {
        var stats = _latestStats;
        return stats.HasValue
            ? [stats.Value.GpuUtilizationPercent]
            : [0f];
    }

    ~NvTegraGpuCollector()
    {
        _tegrastatsProcess?.Kill();
        _tegrastatsProcess?.Dispose();
    }
}
