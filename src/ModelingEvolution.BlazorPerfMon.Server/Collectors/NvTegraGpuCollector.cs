using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Backend.Collectors;

/// <summary>
/// GPU collector for NVIDIA Jetson platforms using tegrastats.
/// Supports Jetson Orin NX, AGX Orin, and other Tegra-based devices.
/// </summary>
public sealed class NvTegraGpuCollector : IGpuCollector
{
    private readonly ILogger<NvTegraGpuCollector> _logger;
    private Process? _tegrastatsProcess;
    private float _latestGpuUtil = 0f;
    private readonly object _lock = new();

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

            // Read output asynchronously
            Task.Run(() =>
            {
                while (_tegrastatsProcess != null && !_tegrastatsProcess.HasExited)
                {
                    var line = _tegrastatsProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        ParseTegrastatsLine(line);
                    }
                }
            });

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
            // Match pattern: GR3D_FREQ 45%
            var match = Regex.Match(line, @"GR3D_FREQ\s+(\d+)%");
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
    /// </summary>
    public float Collect()
    {
        lock (_lock)
        {
            return _latestGpuUtil;
        }
    }

    ~NvTegraGpuCollector()
    {
        _tegrastatsProcess?.Kill();
        _tegrastatsProcess?.Dispose();
    }
}
