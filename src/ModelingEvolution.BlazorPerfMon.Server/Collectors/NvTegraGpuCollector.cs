using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// GPU collector for NVIDIA Jetson platforms using tegrastats.
/// Supports Jetson Orin NX, AGX Orin, and other Tegra-based devices.
/// </summary>
internal sealed class NvTegraGpuCollector : IGpuCollector
{
    private readonly ILogger<NvTegraGpuCollector> _logger;
    private Process? _tegrastatsProcess;
    private float _latestGpuUtil = 0f;
    private readonly object _lock = new();
    private static readonly Regex GpuUtilRegex = new(@"GR3D_FREQ\s+(\d+)%", RegexOptions.Compiled);

    public NvTegraGpuCollector(ILogger<NvTegraGpuCollector> logger)
    {
        _logger = logger;
        StartTegrastats();
    }

    /// <summary>
    /// Starts tegrastats process in background to continuously monitor GPU.
    /// tegrastats output format: "GR3D_FREQ 45%"
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
    /// Parses tegrastats output line to extract GPU utilization.
    /// Example: "RAM 1234/5678MB (lfb 100x4MB) GR3D_FREQ 45%"
    /// </summary>
    private void ParseTegrastatsLine(string line)
    {
        try
        {
            // Match pattern: GR3D_FREQ 45% (using pre-compiled regex)
            var match = GpuUtilRegex.Match(line);
            if (match.Success && float.TryParse(match.Groups[1].Value, out float util))
            {
                lock (_lock)
                {
                    _latestGpuUtil = Math.Clamp(util, 0f, 100f);
                }
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
    /// TODO: Extend to support multiple GPUs on Tegra platforms with multiple GPU clusters.
    /// </summary>
    public float[] Collect()
    {
        lock (_lock)
        {
            return new float[] { _latestGpuUtil };
        }
    }

    ~NvTegraGpuCollector()
    {
        _tegrastatsProcess?.Kill();
        _tegrastatsProcess?.Dispose();
    }
}
