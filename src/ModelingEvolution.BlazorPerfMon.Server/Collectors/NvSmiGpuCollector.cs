using System.Diagnostics;

namespace Backend.Collectors;

/// <summary>
/// GPU collector for desktop NVIDIA GPUs using nvidia-smi.
/// Supports Turing, Ampere, Ada Lovelace architectures.
/// </summary>
public sealed class NvSmiGpuCollector : IGpuCollector
{
    private readonly ILogger<NvSmiGpuCollector> _logger;

    public NvSmiGpuCollector(ILogger<NvSmiGpuCollector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Collects GPU utilization using nvidia-smi.
    /// </summary>
    /// <returns>Single-element array with GPU utilization percentage (0-100)</returns>
    public float[] Collect()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
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
}
