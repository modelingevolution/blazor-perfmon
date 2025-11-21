using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Renders time-series line graph for total average CPU load.
/// Shows historical data with time axis.
/// </summary>
public sealed class TimeSeriesGraphRenderer : IDisposable
{
    private readonly SKPaint _linePaint;
    private readonly SKPaint _fillPaint;
    private readonly SKPaint _textPaint;
    private readonly SKFont _textFont;
    private readonly SKFont _titleFont;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _gridPaint;
    private readonly SKPaint _axisPaint;

    public TimeSeriesGraphRenderer()
    {
        _linePaint = new SKPaint
        {
            Color = new SKColor(100, 255, 100), // Green
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f
        };

        _fillPaint = new SKPaint
        {
            Color = new SKColor(100, 255, 100, 40), // Semi-transparent green
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        _textFont = new SKFont
        {
            Size = 12f
        };

        _titleFont = new SKFont
        {
            Size = 18f
        };

        _textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        _backgroundPaint = new SKPaint
        {
            Color = new SKColor(26, 26, 26), // #1a1a1a
            Style = SKPaintStyle.Fill
        };

        _gridPaint = new SKPaint
        {
            Color = new SKColor(70, 70, 70),
            StrokeWidth = 1f,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke
        };

        _axisPaint = new SKPaint
        {
            Color = new SKColor(150, 150, 150),
            StrokeWidth = 2f,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke
        };
    }

    /// <summary>
    /// Render time-series line graph.
    /// </summary>
    /// <param name="canvas">SKCanvas to draw on</param>
    /// <param name="bounds">Bounds of the rendering area</param>
    /// <param name="dataPoints">Array of data points (0-100)</param>
    /// <param name="maxDataPoints">Maximum number of data points (for time scale)</param>
    public void Render(SKCanvas canvas, SKRect bounds, float[] dataPoints, int maxDataPoints)
    {
        if (dataPoints.Length == 0)
            return;

        canvas.Save();

        try
        {
            // Draw background
            canvas.DrawRect(bounds, _backgroundPaint);

            // Title
            canvas.DrawText("Total Average CPU Load", bounds.Left + 20, bounds.Top + 30, _titleFont, _textPaint);

            // Define graph area
            float marginLeft = 60f;
            float marginRight = 20f;
            float marginTop = 50f;
            float marginBottom = 40f;

            var graphBounds = new SKRect(
                bounds.Left + marginLeft,
                bounds.Top + marginTop,
                bounds.Right - marginRight,
                bounds.Bottom - marginBottom
            );

            // Draw axes
            DrawAxes(canvas, graphBounds);

            // Draw grid
            DrawGrid(canvas, graphBounds);

            // Draw data line
            DrawDataLine(canvas, graphBounds, dataPoints, maxDataPoints);

            // Draw time labels
            DrawTimeLabels(canvas, graphBounds, maxDataPoints);

            // Draw value labels
            DrawValueLabels(canvas, graphBounds);
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawAxes(SKCanvas canvas, SKRect bounds)
    {
        // Y-axis
        canvas.DrawLine(bounds.Left, bounds.Top, bounds.Left, bounds.Bottom, _axisPaint);

        // X-axis
        canvas.DrawLine(bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom, _axisPaint);
    }

    private void DrawGrid(SKCanvas canvas, SKRect bounds)
    {
        // Horizontal grid lines (25%, 50%, 75%, 100%)
        for (int i = 1; i <= 4; i++)
        {
            float y = bounds.Bottom - (bounds.Height * i / 4f);
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, _gridPaint);
        }

        // Vertical grid lines (every 15 seconds for 60 second window)
        for (int i = 1; i < 4; i++)
        {
            float x = bounds.Left + (bounds.Width * i / 4f);
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, _gridPaint);
        }
    }

    private void DrawDataLine(SKCanvas canvas, SKRect bounds, float[] dataPoints, int maxDataPoints)
    {
        if (dataPoints.Length < 2)
            return;

        using var path = new SKPath();
        using var fillPath = new SKPath();

        float xStep = bounds.Width / (maxDataPoints - 1);
        int startIndex = Math.Max(0, dataPoints.Length - maxDataPoints);

        // Start fill path from bottom-left
        fillPath.MoveTo(bounds.Left, bounds.Bottom);

        bool firstPoint = true;
        for (int i = 0; i < dataPoints.Length && i < maxDataPoints; i++)
        {
            float value = Math.Clamp(dataPoints[startIndex + i], 0f, 100f);
            float x = bounds.Left + (i * xStep);
            float y = bounds.Bottom - (bounds.Height * value / 100f);

            if (firstPoint)
            {
                path.MoveTo(x, y);
                fillPath.LineTo(x, y);
                firstPoint = false;
            }
            else
            {
                path.LineTo(x, y);
                fillPath.LineTo(x, y);
            }
        }

        // Complete fill path
        fillPath.LineTo(bounds.Left + ((dataPoints.Length - 1) * xStep), bounds.Bottom);
        fillPath.Close();

        // Draw fill first, then line on top
        canvas.DrawPath(fillPath, _fillPaint);
        canvas.DrawPath(path, _linePaint);
    }

    private void DrawTimeLabels(SKCanvas canvas, SKRect bounds, int maxDataPoints)
    {
        // Time window in seconds (assuming 2Hz = 0.5s per point)
        float totalSeconds = maxDataPoints * 0.5f;

        // Draw labels at 0s, 15s, 30s, 45s, 60s
        for (int i = 0; i <= 4; i++)
        {
            float seconds = totalSeconds * i / 4f;
            float x = bounds.Left + (bounds.Width * i / 4f);
            string label = $"-{totalSeconds - seconds:F0}s";

            canvas.DrawText(label, x - 15, bounds.Bottom + 20, _textFont, _textPaint);
        }
    }

    private void DrawValueLabels(SKCanvas canvas, SKRect bounds)
    {
        // Draw labels at 0%, 25%, 50%, 75%, 100%
        for (int i = 0; i <= 4; i++)
        {
            float value = i * 25f;
            float y = bounds.Bottom - (bounds.Height * i / 4f);
            string label = $"{value:F0}%";

            canvas.DrawText(label, bounds.Left - 45, y + 5, _textFont, _textPaint);
        }
    }

    public void Dispose()
    {
        _linePaint.Dispose();
        _fillPaint.Dispose();
        _textPaint.Dispose();
        _textFont.Dispose();
        _titleFont.Dispose();
        _backgroundPaint.Dispose();
        _gridPaint.Dispose();
        _axisPaint.Dispose();
    }
}
