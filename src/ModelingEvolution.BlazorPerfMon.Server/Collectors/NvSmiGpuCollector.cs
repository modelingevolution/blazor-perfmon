using System.Diagnostics;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// GPU collector for desktop NVIDIA GPUs using nvidia-smi.
/// Supports Turing, Ampere, Ada Lovelace architectures.
/// Collects both utilization and temperature metrics.
/// </summary>
internal sealed class NvSmiGpuCollector : IGpuCollector, ITemperatureCollector
{
    private readonly ILogger<NvSmiGpuCollector> _logger;
    private readonly string? _nvidiaSmiPath;
    private float _lastTemperature = 0f;

    private static readonly string[] NvidiaSmiPaths = new[]
    {
        "nvidia-smi",                    // Standard PATH lookup
        "/usr/bin/nvidia-smi",           // Standard Linux location
        "/usr/lib/wsl/lib/nvidia-smi",   // WSL2 location
        "/usr/local/bin/nvidia-smi"      // Alternative location
    };

    public NvSmiGpuCollector(ILogger<NvSmiGpuCollector> logger)
    {
        _logger = logger;
        _nvidiaSmiPath = FindNvidiaSmi();
        if (_nvidiaSmiPath == null)
        {
            _logger.LogWarning("nvidia-smi not found in any known location");
        }
        else
        {
            _logger.LogInformation("Found nvidia-smi at {Path}", _nvidiaSmiPath);
        }
    }

    private static string? FindNvidiaSmi()
    {
        foreach (var path in NvidiaSmiPaths)
        {
            // For paths with directory, check if file exists
            if (path.Contains('/') && File.Exists(path))
                return path;

            // For bare command, try to find via which
            if (!path.Contains('/'))
            {
                try
                {
                    var whichInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = path,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var process = Process.Start(whichInfo);
                    if (process != null)
                    {
                        var result = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();
                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(result))
                            return result;
                    }
                }
                catch
                {
                    // Ignore errors from which command
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Collects GPU utilization using nvidia-smi.
    /// </summary>
    /// <returns>Single-element array with GPU utilization percentage (0-100)</returns>
    public float[] Collect()
    {
        if (_nvidiaSmiPath == null)
        {
            return new float[] { 0f };
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _nvidiaSmiPath,
                Arguments = "--query-gpu=utilization.gpu --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start nvidia-smi process");
                return new float[] { 0f };
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("nvidia-smi exited with code {ExitCode}. Stderr: {Error}",
                    process.ExitCode, error);
                return new float[] { 0f };
            }

            // Parse output (e.g., "45")
            if (float.TryParse(output.Trim(), out float utilization))
            {
                return new float[] { Math.Clamp(utilization, 0f, 100f) };
            }

            _logger.LogWarning("Failed to parse nvidia-smi output: {Output}", output);
            return new float[] { 0f };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting GPU metrics via nvidia-smi");
            return new float[] { 0f };
        }
    }

    /// <summary>
    /// Collects GPU temperature using nvidia-smi.
    /// Fails fast - throws on any error.
    /// </summary>
    /// <returns>Array with single GPU temperature metric</returns>
    public TemperatureMetric[] CollectTemperatures()
    {
        if (_nvidiaSmiPath == null)
        {
            throw new InvalidOperationException("nvidia-smi not found in any known location");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _nvidiaSmiPath,
            Arguments = "--query-gpu=temperature.gpu --format=csv,noheader,nounits",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start nvidia-smi process for temperature");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"nvidia-smi temperature query exited with code {process.ExitCode}. Stderr: {error}");
        }

        // Parse output (e.g., "45")
        if (!float.TryParse(output.Trim(), out float temperature))
        {
            throw new FormatException($"Failed to parse nvidia-smi temperature output: {output}");
        }

        _lastTemperature = temperature;
        return new[] { new TemperatureMetric { Sensor = "gpu", TempCelsius = temperature } };
    }
}
