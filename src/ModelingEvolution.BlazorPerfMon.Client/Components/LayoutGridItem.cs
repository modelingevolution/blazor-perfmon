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
    /// DEPRECATED: Use TotalColUnits instead for col-span support.
    /// </summary>
    public int ColsInRow { get; init; }

    /// <summary>
    /// Column span value for this grid item (1-12). Determines how many grid units this item occupies.
    /// </summary>
    public uint ColSpan { get; init; } = 1;

    /// <summary>
    /// Cumulative column offset from the start of the row (in grid units).
    /// </summary>
    public uint ColOffset { get; init; }

    /// <summary>
    /// Total column units in this row (sum of all ColSpan values in the row).
    /// </summary>
    public uint TotalColUnits { get; init; }

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
    /// Supports col-span: width is calculated proportional to ColSpan / TotalColUnits.
    /// </summary>
    /// <param name="totalBounds">The total canvas area available for the grid</param>
    /// <returns>The rectangular bounds for this grid cell</returns>
    public SKRect Arrange(in SKRect totalBounds)
    {
        float rowHeight = totalBounds.Height / TotalRows;

        // Use col-span if TotalColUnits is set, otherwise fall back to legacy behavior
        float left, right;
        if (TotalColUnits > 0)
        {
            // Col-span mode: calculate based on ColOffset and ColSpan
            float unitWidth = totalBounds.Width / TotalColUnits;
            left = totalBounds.Left + ColOffset * unitWidth;
            right = totalBounds.Left + (ColOffset + ColSpan) * unitWidth;
        }
        else
        {
            // Legacy mode: equal column widths (backward compatibility)
            float colWidth = totalBounds.Width / ColsInRow;
            left = totalBounds.Left + Col * colWidth;
            right = totalBounds.Left + (Col + 1) * colWidth;
        }

        return new SKRect(
            left,
            totalBounds.Top + Row * rowHeight,
            right,
            totalBounds.Top + (Row + 1) * rowHeight
        );
    }
}
