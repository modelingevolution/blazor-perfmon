using SkiaSharp;

namespace Frontend.Rendering;

/// <summary>
/// Renders horizontal bar chart.
/// </summary>
public sealed class BarChart : ChartBase
{
    private readonly SKPaint _barPaint;
    private readonly SKPaint _barBackgroundPaint;
    private static readonly SKColor BarColor = new SKColor(100, 255, 100); // Green

    private string _title = "Bar Chart";
    private string[] _labels = Array.Empty<string>();
    private float[] _values = Array.Empty<float>();

    public BarChart()
    {
        _barPaint = new SKPaint
        {
            Color = BarColor,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        _barBackgroundPaint = new SKPaint
        {
            Color = new SKColor(50, 50, 50),
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };
    }

    /// <summary>
    /// Sets the data for the bar chart.
    /// </summary>
    /// <param name="title">Chart title</param>
    /// <param name="labels">Labels for each bar</param>
    /// <param name="values">Values for each bar (0-100)</param>
    public void SetData(string title, string[] labels, float[] values)
    {
        _title = title;
        _labels = labels;
        _values = values;
    }

    protected override void RenderContent(SKCanvas canvas)
    {
        if (_values.Length == 0)
            return;

        var bounds = Bounds;

        // Title
        canvas.DrawText(_title, bounds.Left + 20, bounds.Top + 30, TitleFont, TextPaint);

        // Calculate bar dimensions
        float barAreaTop = bounds.Top + 50;
        float barAreaHeight = bounds.Height - 60;
        int barCount = _values.Length;

        float barHeight = Math.Min(barAreaHeight / barCount - 4, 40); // Max 40px per bar
        float barSpacing = 4f;

        float labelWidth = 80f;
        float barStartX = bounds.Left + labelWidth;
        float barWidth = bounds.Width - labelWidth - 120f; // Leave space for percentage

        // Draw vertical grid lines (25%, 50%, 75%, 100%)
        for (int i = 1; i <= 4; i++)
        {
            float x = barStartX + (barWidth * i / 4f);
            canvas.DrawLine(x, barAreaTop, x, barAreaTop + (barHeight + barSpacing) * barCount, GridPaint);
        }

        for (int i = 0; i < barCount; i++)
        {
            float y = barAreaTop + i * (barHeight + barSpacing);
            float value = Math.Clamp(_values[i], 0f, 100f);

            // Draw bar background
            var barBgRect = new SKRect(barStartX, y, barStartX + barWidth, y + barHeight);
            canvas.DrawRect(barBgRect, _barBackgroundPaint);

            // Draw bar foreground (green)
            float fillWidth = barWidth * (value / 100f);
            if (fillWidth > 0)
            {
                var barRect = new SKRect(barStartX, y, barStartX + fillWidth, y + barHeight);
                canvas.DrawRect(barRect, _barPaint);
            }

            // Draw label
            string label = i < _labels.Length ? _labels[i] : $"Item {i}";
            canvas.DrawText(label, bounds.Left + 20, y + barHeight / 2 + 5, TextFont, TextPaint);

            // Draw percentage
            string percentage = $"{value:F1}%";
            canvas.DrawText(percentage, barStartX + barWidth + 10, y + barHeight / 2 + 5, TextFont, TextPaint);
        }
    }

    public override void Dispose()
    {
        _barPaint.Dispose();
        _barBackgroundPaint.Dispose();
        base.Dispose();
    }
}
