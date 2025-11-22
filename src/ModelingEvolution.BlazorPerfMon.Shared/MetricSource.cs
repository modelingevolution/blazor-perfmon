using MessagePack;
using ModelingEvolution;
using ModelingEvolution.JsonParsableConverter;
using System.Text.Json.Serialization;

namespace ModelingEvolution.BlazorPerfMon.Shared;

/// <summary>
/// Describes a single metric source (CPU, GPU, Network interface, Disk, etc.).
/// Used in PerformanceConfigurationSnapshot to inform the client about available metrics.
/// </summary>
[MessagePackObject]
[JsonConverter(typeof(JsonParsableConverter<MetricSource>))]
public readonly record struct MetricSource : IParsable<MetricSource>
{
    /// <summary>
    /// Metric type name (e.g., "CPU", "GPU", "Network", "Disk", "RAM").
    /// </summary>
    [Key(0)]
    public string Name { get; init; }

    /// <summary>
    /// Optional identifier for this specific instance (e.g., "eth0", "/dev/sda", "GPU0").
    /// Null for singleton metrics like CPU or RAM.
    /// </summary>
    [Key(1)]
    public string? Identifier { get; init; }

    /// <summary>
    /// Number of data points for this metric (e.g., 16 CPU cores, 2 network interfaces).
    /// For aggregated metrics, this is 1.
    /// </summary>
    [Key(2)]
    public uint Count { get; init; }

    /// <summary>
    /// Number of grid columns this metric should span (1-12). Defaults to 1.
    /// </summary>
    [Key(3)]
    public uint ColSpan { get; init; }

    public override string ToString()
    {
        var baseFormat = Identifier is null ? $"{Name}/{Count}" : $"{Name}:{Identifier}/{Count}";
        return ColSpan > 1 ? $"{baseFormat}|col-span:{ColSpan}" : baseFormat;
    }

    public static MetricSource Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"Invalid MetricSource format: {s}. Expected format: 'Name', 'Name/Count', 'Name:Identifier', 'Name:Identifier/Count', or append '|col-span:N' (1-12)");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out MetricSource result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        // Step 1: Split by '|' to separate base format from col-span (optional whitespace allowed)
        var pipeParts = s.Split('|');
        var baseFormat = pipeParts[0].Trim();
        uint colSpan = 1; // Default col-span

        if (pipeParts.Length == 2)
        {
            // Parse col-span part: "col-span:N" (with optional whitespace)
            var colSpanPart = pipeParts[1].Trim();
            var colSpanKeyValue = colSpanPart.Split(':');

            if (colSpanKeyValue.Length != 2)
                return false;

            var key = colSpanKeyValue[0].Trim();
            var value = colSpanKeyValue[1].Trim();

            if (!key.Equals("col-span", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!uint.TryParse(value, out colSpan))
                return false;

            // Validate col-span range (1-12)
            if (colSpan < 1 || colSpan > 12)
                return false;
        }
        else if (pipeParts.Length != 1)
        {
            // Invalid format - too many '|' characters
            return false;
        }

        // Step 2: Parse base format (Name:Identifier/Count)
        // Split by '/' to get name/identifier part and count
        var parts = baseFormat.Split('/');

        uint count = 1; // Default count for metrics without explicit count

        if (parts.Length == 2)
        {
            // Format: "Name/Count" or "Name:Identifier/Count"
            if (!uint.TryParse(parts[1].Trim(), out count))
                return false;
        }
        else if (parts.Length != 1)
        {
            // Invalid format - too many '/' characters
            return false;
        }
        // else: parts.Length == 1, Format: "Name" (count defaults to 1)

        // Split name/identifier part by ':'
        var nameParts = parts[0].Split(':');

        if (nameParts.Length == 1)
        {
            // Format: "Name" or "Name/Count"
            result = new MetricSource
            {
                Name = nameParts[0].Trim(),
                Identifier = null,
                Count = count,
                ColSpan = colSpan
            };
            return true;
        }
        else if (nameParts.Length == 2)
        {
            // Format: "Name:Identifier" or "Name:Identifier/Count"
            result = new MetricSource
            {
                Name = nameParts[0].Trim(),
                Identifier = nameParts[1].Trim(),
                Count = count,
                ColSpan = colSpan
            };
            return true;
        }

        return false;
    }
}
