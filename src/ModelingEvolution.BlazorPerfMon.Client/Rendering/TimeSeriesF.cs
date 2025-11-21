using SkiaSharp;

namespace Frontend.Rendering;

/// <summary>
/// Represents a single time series for rendering in a chart.
/// </summary>
public readonly struct TimeSeriesF
{
    /// <summary>
    /// Label/name of the series (e.g., "CPU", "GPU", "RAM").
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Data points as IEnumerable for zero-copy enumeration.
    /// </summary>
    public required IEnumerable<float> Data { get; init; }

    /// <summary>
    /// Number of data points (pre-counted to avoid multiple enumerations).
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Color for rendering this series.
    /// </summary>
    public required SKColor Color { get; init; }
}
