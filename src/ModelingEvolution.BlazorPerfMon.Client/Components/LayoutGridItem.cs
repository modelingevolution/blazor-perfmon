using ModelingEvolution.BlazorPerfMon.Client.Rendering;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Components;

/// <summary>
/// Represents a single chart positioned within a grid layout.
/// Responsible for calculating its own bounds based on grid position and total grid dimensions.
/// </summary>
public sealed class LayoutGridItem
{
    /// <summary>
    /// Zero-based row index in the grid.
    /// </summary>
    public int Row { get; init; }

    /// <summary>
    /// Zero-based column index within the row.
    /// </summary>
    public int Col { get; init; }

    /// <summary>
    /// Total number of rows in the entire grid.
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Number of columns in this row (rows can have different column counts).
    /// </summary>
    public int ColsInRow { get; init; }

    /// <summary>
    /// Metadata about the metric source being displayed (for debugging/logging).
    /// </summary>
    public MetricSource Source { get; init; }

    /// <summary>
    /// The chart instance to render.
    /// </summary>
    public IChart Chart { get; init; } = null!;

    /// <summary>
    /// Calculates this grid item's bounds within the total canvas bounds.
    /// </summary>
    /// <param name="totalBounds">The total canvas area available for the grid</param>
    /// <returns>The rectangular bounds for this grid cell</returns>
    public SKRect Arrange(in SKRect totalBounds)
    {
        float rowHeight = totalBounds.Height / TotalRows;
        float colWidth = totalBounds.Width / ColsInRow;

        return new SKRect(
            totalBounds.Left + Col * colWidth,
            totalBounds.Top + Row * rowHeight,
            totalBounds.Left + (Col + 1) * colWidth,
            totalBounds.Top + (Row + 1) * rowHeight
        );
    }
}
