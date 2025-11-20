using SkiaSharp;

namespace Frontend.Rendering;

/// <summary>
/// Renders CPU load as horizontal bars.
/// Shows current load per CPU core plus total average load.
/// </summary>
public sealed class CpuGraphRenderer : IDisposable
{
    // Green color for all CPU bars
    private static readonly SKColor CpuBarColor = new SKColor(100, 255, 100); // Green

    private readonly SKPaint _barPaint;
    private readonly SKPaint _barBackgroundPaint;
    private readonly SKPaint _totalBarPaint;
    private readonly SKPaint _textPaint;
    private readonly SKFont _textFont;
    private readonly SKFont _titleFont;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _gridPaint;

    public CpuGraphRenderer()
    {
        _barPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        _barBackgroundPaint = new SKPaint
        {
            Color = new SKColor(50, 50, 50),
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };

        _totalBarPaint = new SKPaint
        {
            Color = new SKColor(100, 200, 255),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        _textFont = new SKFont
        {
            Size = 14f
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
    }

    /// <summary>
    /// Render CPU load bars.
    /// </summary>
    /// <param name="canvas">SKCanvas to draw on</param>
    /// <param name="bounds">Bounds of the rendering area</param>
    /// <param name="currentLoads">Current CPU load values (0-100)</param>
    public void Render(SKCanvas canvas, SKRect bounds, float[] currentLoads)
    {
        if (currentLoads.Length == 0)
            return;

        canvas.Save();

        try
        {
            // Draw background
            canvas.DrawRect(bounds, _backgroundPaint);

            // Draw individual CPU bars
            DrawIndividualCpuBars(canvas, bounds, currentLoads);
        }
        finally
        {
            canvas.Restore();
        }
    }

    private void DrawIndividualCpuBars(SKCanvas canvas, SKRect bounds, float[] loads)
    {
        // Title
        canvas.DrawText("CPU Cores", bounds.Left + 20, bounds.Top + 30, _titleFont, _textPaint);

        // Calculate bar dimensions
        float barAreaTop = bounds.Top + 50;
        float barAreaHeight = bounds.Height - 60;
        int coreCount = loads.Length;

        float barHeight = Math.Min(barAreaHeight / coreCount - 4, 40); // Max 40px per bar
        float barSpacing = 4f;

        float labelWidth = 80f;
        float barStartX = bounds.Left + labelWidth;
        float barWidth = bounds.Width - labelWidth - 120f; // Leave space for percentage

        // Draw vertical grid lines (25%, 50%, 75%, 100%)
        for (int i = 1; i <= 4; i++)
        {
            float x = barStartX + (barWidth * i / 4f);
            canvas.DrawLine(x, barAreaTop, x, barAreaTop + (barHeight + barSpacing) * coreCount, _gridPaint);
        }

        for (int i = 0; i < coreCount; i++)
        {
            float y = barAreaTop + i * (barHeight + barSpacing);
            float load = Math.Clamp(loads[i], 0f, 100f);

            // Draw bar background
            var barBgRect = new SKRect(barStartX, y, barStartX + barWidth, y + barHeight);
            canvas.DrawRect(barBgRect, _barBackgroundPaint);

            // Draw bar foreground (green)
            float fillWidth = barWidth * (load / 100f);
            if (fillWidth > 0)
            {
                _barPaint.Color = CpuBarColor;
                var barRect = new SKRect(barStartX, y, barStartX + fillWidth, y + barHeight);
                canvas.DrawRect(barRect, _barPaint);
            }

            // Draw label
            string label = $"CPU{i}";
            canvas.DrawText(label, bounds.Left + 20, y + barHeight / 2 + 5, _textFont, _textPaint);

            // Draw percentage
            string percentage = $"{load:F1}%";
            canvas.DrawText(percentage, barStartX + barWidth + 10, y + barHeight / 2 + 5, _textFont, _textPaint);
        }
    }


    public void Dispose()
    {
        _barPaint.Dispose();
        _barBackgroundPaint.Dispose();
        _totalBarPaint.Dispose();
        _textPaint.Dispose();
        _textFont.Dispose();
        _titleFont.Dispose();
        _backgroundPaint.Dispose();
        _gridPaint.Dispose();
    }
}
