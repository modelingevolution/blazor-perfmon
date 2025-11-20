using SkiaSharp;

namespace Frontend.Rendering;

/// <summary>
/// Abstract base class for all chart renderers.
/// Provides common rendering infrastructure and positioning.
/// </summary>
public abstract class ChartBase : IDisposable
{
    protected readonly SKPaint TextPaint;
    protected readonly SKFont TextFont;
    protected readonly SKFont TitleFont;
    protected readonly SKPaint BackgroundPaint;
    protected readonly SKPaint GridPaint;

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

    protected ChartBase()
    {
        TextFont = new SKFont
        {
            Size = 16f
        };

        TitleFont = new SKFont
        {
            Size = 24f
        };

        TextPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        BackgroundPaint = new SKPaint
        {
            Color = new SKColor(26, 26, 26), // #1a1a1a
            Style = SKPaintStyle.Fill
        };

        GridPaint = new SKPaint
        {
            Color = new SKColor(70, 70, 70),
            StrokeWidth = 1f,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke
        };
    }

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
        TextPaint.Dispose();
        TextFont.Dispose();
        TitleFont.Dispose();
        BackgroundPaint.Dispose();
        GridPaint.Dispose();
    }
}
