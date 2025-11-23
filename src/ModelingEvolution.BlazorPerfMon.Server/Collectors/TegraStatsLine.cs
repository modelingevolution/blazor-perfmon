using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ModelingEvolution.BlazorPerfMon.Server.Collectors;

/// <summary>
/// Represents memory information from tegrastats RAM field.
/// Example: "RAM 1729/15657MB (lfb 8x4MB)"
/// </summary>
public readonly record struct RamInfo : IParsable<RamInfo>
{
    public int UsedMB { get; init; }
    public int TotalMB { get; init; }
    public int LowFragBufferCount { get; init; }
    public int LowFragBufferSizeMB { get; init; }

    public float UsagePercent => TotalMB > 0 ? (UsedMB * 100f) / TotalMB : 0f;

    public static RamInfo Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid RAM format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out RamInfo result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            // Format: "1729/15657MB (lfb 8x4MB)"
            // Must parse from end to avoid ambiguity with multiple "MB"

            var span = s.AsSpan().Trim();
            if (!span.EndsWith("MB)", StringComparison.Ordinal))
                return false;

            // Find closing paren - work backwards
            var closeParenIdx = span.LastIndexOf(')');
            var openParenIdx = span.LastIndexOf('(');
            if (openParenIdx < 0 || closeParenIdx < 0) return false;

            // Extract lfb section: "(lfb 8x4MB)"
            var lfbSection = span.Slice(openParenIdx + 1, closeParenIdx - openParenIdx - 1);
            if (!lfbSection.StartsWith("lfb ", StringComparison.Ordinal))
                return false;

            var lfbData = lfbSection.Slice(4); // Skip "lfb "
            var xIdx = lfbData.IndexOf('x');
            if (xIdx < 0) return false;

            var lfbMbIdx = lfbData.IndexOf("MB", StringComparison.Ordinal);
            if (lfbMbIdx < 0) return false;

            var lfbCount = int.Parse(lfbData.Slice(0, xIdx), CultureInfo.InvariantCulture);
            var lfbSize = int.Parse(lfbData.Slice(xIdx + 1, lfbMbIdx - xIdx - 1), CultureInfo.InvariantCulture);

            // Extract main section: "1729/15657MB"
            var mainSection = span.Slice(0, openParenIdx).TrimEnd();
            if (!mainSection.EndsWith("MB", StringComparison.Ordinal))
                return false;

            mainSection = mainSection.Slice(0, mainSection.Length - 2); // Remove "MB"
            var slashIdx = mainSection.IndexOf('/');
            if (slashIdx < 0) return false;

            var usedMB = int.Parse(mainSection.Slice(0, slashIdx), CultureInfo.InvariantCulture);
            var totalMB = int.Parse(mainSection.Slice(slashIdx + 1), CultureInfo.InvariantCulture);

            result = new RamInfo
            {
                UsedMB = usedMB,
                TotalMB = totalMB,
                LowFragBufferCount = lfbCount,
                LowFragBufferSizeMB = lfbSize
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents swap memory information from tegrastats.
/// Example: "SWAP 0/7828MB (cached 0MB)"
/// </summary>
public readonly record struct SwapInfo : IParsable<SwapInfo>
{
    public int UsedMB { get; init; }
    public int TotalMB { get; init; }
    public int CachedMB { get; init; }

    public float UsagePercent => TotalMB > 0 ? (UsedMB * 100f) / TotalMB : 0f;

    public static SwapInfo Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid SWAP format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SwapInfo result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            // Format: "0/7828MB (cached 0MB)"
            // Parse from end to avoid ambiguity

            var span = s.AsSpan().Trim();
            if (!span.EndsWith("MB)", StringComparison.Ordinal))
                return false;

            // Find closing paren - work backwards
            var closeParenIdx = span.LastIndexOf(')');
            var openParenIdx = span.LastIndexOf('(');
            if (openParenIdx < 0 || closeParenIdx < 0) return false;

            // Extract cached section: "(cached 0MB)"
            var cachedSection = span.Slice(openParenIdx + 1, closeParenIdx - openParenIdx - 1);
            if (!cachedSection.StartsWith("cached ", StringComparison.Ordinal))
                return false;

            var cachedData = cachedSection.Slice(7); // Skip "cached "
            if (!cachedData.EndsWith("MB", StringComparison.Ordinal))
                return false;

            var cachedMB = int.Parse(cachedData.Slice(0, cachedData.Length - 2), CultureInfo.InvariantCulture);

            // Extract main section: "0/7828MB"
            var mainSection = span.Slice(0, openParenIdx).TrimEnd();
            if (!mainSection.EndsWith("MB", StringComparison.Ordinal))
                return false;

            mainSection = mainSection.Slice(0, mainSection.Length - 2); // Remove "MB"
            var slashIdx = mainSection.IndexOf('/');
            if (slashIdx < 0) return false;

            var usedMB = int.Parse(mainSection.Slice(0, slashIdx), CultureInfo.InvariantCulture);
            var totalMB = int.Parse(mainSection.Slice(slashIdx + 1), CultureInfo.InvariantCulture);

            result = new SwapInfo
            {
                UsedMB = usedMB,
                TotalMB = totalMB,
                CachedMB = cachedMB
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a single CPU core state from tegrastats.
/// Example: "1%@1420" (online) or "off" (offline)
/// </summary>
public readonly record struct CpuCoreState : IParsable<CpuCoreState>
{
    public bool IsOnline { get; init; }
    public int UtilizationPercent { get; init; }
    public int FrequencyMHz { get; init; }

    public static CpuCoreState Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid CPU core format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CpuCoreState result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        var trimmed = s.Trim();

        if (trimmed.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            result = new CpuCoreState { IsOnline = false };
            return true;
        }

        try
        {
            // Format: "1%@1420"
            var percentIdx = trimmed.IndexOf('%');
            if (percentIdx < 0) return false;

            var atIdx = trimmed.IndexOf('@', percentIdx);
            if (atIdx < 0) return false;

            var util = int.Parse(trimmed.AsSpan(0, percentIdx), CultureInfo.InvariantCulture);
            var freq = int.Parse(trimmed.AsSpan(atIdx + 1), CultureInfo.InvariantCulture);

            result = new CpuCoreState
            {
                IsOnline = true,
                UtilizationPercent = util,
                FrequencyMHz = freq
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents CPU information with per-core states from tegrastats.
/// Example: "CPU [1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off]"
/// </summary>
public readonly record struct CpuInfo : IParsable<CpuInfo>
{
    public ImmutableArray<CpuCoreState> Cores { get; init; }

    public int OnlineCoreCount => Cores.Count(c => c.IsOnline);
    public float AverageUtilization => OnlineCoreCount > 0
        ? (float)Cores.Where(c => c.IsOnline).Average(c => c.UtilizationPercent)
        : 0f;

    public static CpuInfo Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid CPU format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CpuInfo result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        var trimmed = s.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']'))
            return false;

        try
        {
            // Format: "[1%@1420,2%@1420,...]"
            var coreStates = trimmed[1..^1].Split(',', StringSplitOptions.TrimEntries);
            var cores = new CpuCoreState[coreStates.Length];

            for (int i = 0; i < coreStates.Length; i++)
            {
                if (!CpuCoreState.TryParse(coreStates[i], provider, out cores[i]))
                    return false;
            }

            result = new CpuInfo { Cores = cores.ToImmutableArray() };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a temperature sensor reading from tegrastats.
/// Example: "cv0@43.562C" or "cpu@46.125C"
/// </summary>
public readonly record struct TemperatureReading : IParsable<TemperatureReading>
{
    public string Sensor { get; init; }
    public float TempCelsius { get; init; }

    public static TemperatureReading Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid temperature format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TemperatureReading result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        var trimmed = s.Trim();
        if (!trimmed.EndsWith('C'))
            return false;

        try
        {
            // Format: "cpu@46.125C"
            var atIdx = trimmed.IndexOf('@');
            if (atIdx <= 0) return false; // Need at least one char for sensor name

            var sensor = trimmed[..atIdx];
            var temp = float.Parse(trimmed.AsSpan(atIdx + 1, trimmed.Length - atIdx - 2), CultureInfo.InvariantCulture);

            result = new TemperatureReading
            {
                Sensor = sensor,
                TempCelsius = temp
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a power rail reading from tegrastats.
/// Example: "VDD_IN 6120mW/6120mW" (current/average)
/// </summary>
public readonly record struct PowerRailReading : IParsable<PowerRailReading>
{
    public string Name { get; init; }
    public int CurrentMilliwatts { get; init; }
    public int AverageMilliwatts { get; init; }

    public static PowerRailReading Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid power rail format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out PowerRailReading result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            // Format: "VDD_IN 6120mW/6120mW"
            // Parse explicitly to find both "mW" markers

            var span = s.AsSpan().Trim();
            if (!span.EndsWith("mW", StringComparison.Ordinal))
                return false;

            var spaceIdx = span.IndexOf(' ');
            if (spaceIdx < 0) return false;

            var slashIdx = span.Slice(spaceIdx).IndexOf('/');
            if (slashIdx < 0) return false;
            slashIdx += spaceIdx; // Adjust to absolute position

            var name = span.Slice(0, spaceIdx).ToString();

            // Find first "mW" after space
            var firstMwIdx = span.Slice(spaceIdx).IndexOf("mW", StringComparison.Ordinal);
            if (firstMwIdx < 0) return false;
            firstMwIdx += spaceIdx; // Adjust to absolute position

            // Find second "mW" after slash
            var secondMwIdx = span.Slice(slashIdx).IndexOf("mW", StringComparison.Ordinal);
            if (secondMwIdx < 0) return false;
            secondMwIdx += slashIdx; // Adjust to absolute position

            var current = int.Parse(span.Slice(spaceIdx + 1, firstMwIdx - spaceIdx - 1), CultureInfo.InvariantCulture);
            var average = int.Parse(span.Slice(slashIdx + 1, secondMwIdx - slashIdx - 1), CultureInfo.InvariantCulture);

            result = new PowerRailReading
            {
                Name = name,
                CurrentMilliwatts = current,
                AverageMilliwatts = average
            };
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a complete line from tegrastats output with all metrics.
/// Example line:
/// "11-23-2025 22:47:56 RAM 1729/15657MB (lfb 8x4MB) SWAP 0/7828MB (cached 0MB) CPU [1%@1420,2%@1420,4%@1420,3%@1420,off,off,off,off] GR3D_FREQ 0% cv0@43.562C cpu@46.125C soc2@43.781C soc0@44.656C cv1@43.312C gpu@43C tj@46.937C soc1@46.937C cv2@43.781C VDD_IN 6120mW/6120mW VDD_CPU_GPU_CV 436mW/436mW VDD_SOC 2464mW/2464mW"
/// </summary>
public readonly record struct TegraStatsLine : IParsable<TegraStatsLine>
{
    public DateTime Timestamp { get; init; }
    public RamInfo Ram { get; init; }
    public SwapInfo Swap { get; init; }
    public CpuInfo Cpu { get; init; }
    public int GpuUtilizationPercent { get; init; }
    public ImmutableArray<TemperatureReading> Temperatures { get; init; }
    public ImmutableArray<PowerRailReading> PowerRails { get; init; }

    public static TegraStatsLine Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
            throw new FormatException($"Invalid tegrastats line format: {s}");
        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TegraStatsLine result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            // Format: "11-23-2025 22:47:56 RAM ... SWAP ... CPU [...] GR3D_FREQ 0% sensors... VDD_rails..."

            // Parse timestamp (first 19 chars: "MM-dd-yyyy HH:mm:ss")
            if (s.Length < 20) return false;
            var timestamp = DateTime.ParseExact(
                s[..19],
                "MM-dd-yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal);

            // Parse RAM section
            var ramIdx = s.IndexOf("RAM ", StringComparison.Ordinal);
            if (ramIdx < 0) return false;
            var ramEnd = s.IndexOf("MB)", ramIdx, StringComparison.Ordinal);
            if (ramEnd < 0) return false;
            if (!RamInfo.TryParse(s[(ramIdx + 4)..(ramEnd + 3)], provider, out var ram))
                return false;

            // Parse SWAP section
            var swapIdx = s.IndexOf("SWAP ", StringComparison.Ordinal);
            if (swapIdx < 0) return false;
            var swapEnd = s.IndexOf("MB)", swapIdx, StringComparison.Ordinal);
            if (swapEnd < 0) return false;
            if (!SwapInfo.TryParse(s[(swapIdx + 5)..(swapEnd + 3)], provider, out var swap))
                return false;

            // Parse CPU section
            var cpuIdx = s.IndexOf("CPU [", StringComparison.Ordinal);
            if (cpuIdx < 0) return false;
            var cpuEnd = s.IndexOf(']', cpuIdx);
            if (cpuEnd < 0) return false;
            if (!CpuInfo.TryParse(s[(cpuIdx + 4)..(cpuEnd + 1)], provider, out var cpu))
                return false;

            // Parse GPU
            var gpuIdx = s.IndexOf("GR3D_FREQ ", StringComparison.Ordinal);
            if (gpuIdx < 0) return false;
            var gpuPercent = s.IndexOf('%', gpuIdx);
            if (gpuPercent < 0) return false;
            if (!int.TryParse(s.AsSpan(gpuIdx + 10, gpuPercent - gpuIdx - 10), out var gpuUtil))
                return false;

            // Parse temperatures (all strings matching pattern "name@temp.C")
            var temps = ImmutableArray.CreateBuilder<TemperatureReading>();
            var searchIdx = gpuPercent;
            while (true)
            {
                var atIdx = s.IndexOf('@', searchIdx);
                if (atIdx < 0) break;

                var cIdx = s.IndexOf('C', atIdx);
                if (cIdx < 0 || cIdx - atIdx > 10) break; // reasonable temp length limit

                // Find start of sensor name (last space before @)
                var spaceIdx = s.LastIndexOf(' ', atIdx);
                if (spaceIdx < 0 || spaceIdx < searchIdx) break;

                // Check if next char after 'C' is space or VDD (power rail start)
                if (cIdx + 1 < s.Length && s[cIdx + 1] != ' ')
                {
                    searchIdx = cIdx + 1;
                    continue;
                }

                if (TemperatureReading.TryParse(s[(spaceIdx + 1)..(cIdx + 1)], provider, out var temp))
                {
                    temps.Add(temp);
                    searchIdx = cIdx + 1;
                }
                else
                {
                    break;
                }
            }

            // Parse power rails (all strings matching "VDD_NAME currentmW/averagemW")
            var powerRails = ImmutableArray.CreateBuilder<PowerRailReading>();
            var vddIdx = 0;
            while (true)
            {
                vddIdx = s.IndexOf("VDD_", vddIdx, StringComparison.Ordinal);
                if (vddIdx < 0) break;

                var vddEnd = s.IndexOf("mW", vddIdx, StringComparison.Ordinal);
                if (vddEnd < 0) break;
                vddEnd = s.IndexOf("mW", vddEnd + 2, StringComparison.Ordinal); // Find second mW
                if (vddEnd < 0) break;

                if (PowerRailReading.TryParse(s[vddIdx..(vddEnd + 2)], provider, out var power))
                {
                    powerRails.Add(power);
                    vddIdx = vddEnd + 2;
                }
                else
                {
                    break;
                }
            }

            result = new TegraStatsLine
            {
                Timestamp = timestamp,
                Ram = ram,
                Swap = swap,
                Cpu = cpu,
                GpuUtilizationPercent = gpuUtil,
                Temperatures = temps.ToImmutable(),
                PowerRails = powerRails.ToImmutable()
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets temperature reading for a specific sensor.
    /// </summary>
    public float? GetTemperature(string sensorName)
    {
        var temp = Temperatures.FirstOrDefault(t => t.Sensor.Equals(sensorName, StringComparison.OrdinalIgnoreCase));
        return temp.Sensor != null ? temp.TempCelsius : null;
    }

    /// <summary>
    /// Gets power reading for a specific rail.
    /// </summary>
    public PowerRailReading? GetPowerRail(string railName)
    {
        var power = PowerRails.FirstOrDefault(p => p.Name.Equals(railName, StringComparison.OrdinalIgnoreCase));
        return power.Name != null ? power : null;
    }

    /// <summary>
    /// Calculates total power consumption across all rails (current values).
    /// </summary>
    public int TotalPowerMilliwatts => PowerRails.Sum(p => p.CurrentMilliwatts);
}
