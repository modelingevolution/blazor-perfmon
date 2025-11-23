using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Abstract base class for all chart renderers.
/// Provides common rendering infrastructure and positioning.
/// </summary>
public abstract class ChartBase : IDisposable
{
    /// <summary>
    /// Unified text paint - use ChartStyles.Text.
    /// </summary>
    protected readonly SKPaint TextPaint = ChartStyles.Text;

    /// <summary>
    /// Unified label font - use ChartStyles.LabelFont.
    /// </summary>
    protected readonly SKFont TextFont = ChartStyles.LabelFont;

    /// <summary>
    /// Unified title font - use ChartStyles.TitleFont.
    /// </summary>
    protected readonly SKFont TitleFont = ChartStyles.TitleFont;

    /// <summary>
    /// Unified background paint - use ChartStyles.Background.
    /// </summary>
    protected readonly SKPaint BackgroundPaint = ChartStyles.Background;

    /// <summary>
    /// Unified grid paint - use ChartStyles.Grid.
    /// </summary>
    protected readonly SKPaint GridPaint = ChartStyles.Grid;

    /// <summary>
    /// Location of the chart on the canvas (top-left corner).
    /// </summary>
    public SKPoint Location { get; set; }

    /// <summary>
    /// Size of the chart on the canvas.
    /// </summary>
    public SKSize Size { get; set; }

    /// <summary>
    /// Gets the bounds rectangle for this chart.
    /// </summary>
    public SKRect Bounds => new SKRect(Location.X, Location.Y, Location.X + Size.Width, Location.Y + Size.Height);

    /// <summary>
    /// Renders the chart on the given canvas.
    /// </summary>
    /// <param name="canvas">Canvas to draw on</param>
    public void Render(SKCanvas canvas)
    {
        canvas.Save();
        try
        {
            // Draw background
            canvas.DrawRect(Bounds, BackgroundPaint);

            // Render chart content
            RenderContent(canvas);
        }
        finally
        {
            canvas.Restore();
        }
    }

    /// <summary>
    /// Renders the chart content. Override in derived classes.
    /// </summary>
    /// <param name="canvas">Canvas to draw on</param>
    protected abstract void RenderContent(SKCanvas canvas);

    public virtual void Dispose()
    {
        // ChartBase now uses shared static paints from ChartStyles
        // No disposal needed for shared resources
    }
}
