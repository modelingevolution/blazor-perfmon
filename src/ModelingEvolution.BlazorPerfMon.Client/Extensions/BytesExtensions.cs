using ModelingEvolution;

namespace ModelingEvolution.BlazorPerfMon.Client.Extensions;

/// <summary>
/// Extension methods for Bytes type formatting.
/// </summary>
public static class BytesExtensions
{
    /// <summary>
    /// Formats bytes with fixed width for consistent display in charts.
    /// Converts "bytes" to " B" and pads to 8 characters.
    /// </summary>
    public static string FormatFixed(this Bytes value)
    {
        string formatted = value.ToString().Replace("bytes", " B");
        // Pad left to 8 characters for consistent width (e.g., "123.4 KB")
        return formatted.PadLeft(8);
    }
}
