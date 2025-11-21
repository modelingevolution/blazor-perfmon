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

    public override string ToString()
    {
        return Identifier is null ? $"{Name}/{Count}" : $"{Name}.{Identifier}/{Count}";
    }

    public static MetricSource Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"Invalid MetricSource format: {s}. Expected format: 'Name/Count' or 'Name.Identifier/Count'");
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out MetricSource result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        // Split by '/' to get name/identifier part and count
        var parts = s.Split('/');
        if (parts.Length != 2)
            return false;

        if (!uint.TryParse(parts[1], out var count))
            return false;

        // Split name/identifier part by '.'
        var nameParts = parts[0].Split('.');

        if (nameParts.Length == 1)
        {
            // Format: "Name/Count"
            result = new MetricSource
            {
                Name = nameParts[0],
                Identifier = null,
                Count = count
            };
            return true;
        }
        else if (nameParts.Length == 2)
        {
            // Format: "Name.Identifier/Count"
            result = new MetricSource
            {
                Name = nameParts[0],
                Identifier = nameParts[1],
                Count = count
            };
            return true;
        }

        return false;
    }
}
