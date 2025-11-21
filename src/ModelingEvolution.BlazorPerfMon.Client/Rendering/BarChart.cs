using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Renders horizontal bar chart.
/// </summary>
public sealed class BarChart : ChartBase
{
    private readonly SKPaint _barPaint;
    private readonly SKPaint _barBackgroundPaint;
    private static readonly SKColor BarColor = new SKColor(100, 255, 100); // Green

    private string _title = "Bar Chart";
    private IEnumerable<string> _labels = Enumerable.Empty<string>();
    private int _labelCount = 0;
    private IEnumerable<float> _values = Enumerable.Empty<float>();
    private int _valueCount = 0;

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
    /// Accepts IEnumerable for zero-copy architecture.
    /// </summary>
    /// <param name="title">Chart title</param>
    /// <param name="labels">Labels for each bar</param>
    /// <param name="labelCount">Number of labels</param>
    /// <param name="values">Values for each bar (0-100)</param>
    /// <param name="valueCount">Number of values</param>
    public void SetData(string title, IEnumerable<string> labels, int labelCount, IEnumerable<float> values, int valueCount)
    {
        _title = title;
        _labels = labels;
        _labelCount = labelCount;
        _values = values;
        _valueCount = valueCount;
    }

    protected override void RenderContent(SKCanvas canvas)
    {
        if (_valueCount == 0)
            return;

        var bounds = Bounds;

        // Title
        canvas.DrawText(_title, bounds.Left + 20, bounds.Top + 30, TitleFont, TextPaint);

        // Calculate bar dimensions
        float barAreaTop = bounds.Top + 50;
        float barAreaHeight = bounds.Height - 60;
        int barCount = _valueCount;

        float barHeight = Math.Min(barAreaHeight / barCount - 4, 40); // Max 40px per bar
        float barSpacing = 4f;

        float labelWidth = 80f;
        float barStartX = bounds.Left + labelWidth;
        float barWidth = bounds.Width - labelWidth - 120f; // Leave space for percentage

        // Draw vertical grid lines (25%, 50%, 75%, 100%)
        for (int j = 1; j <= 4; j++)
        {
            float x = barStartX + (barWidth * j / 4f);
            canvas.DrawLine(x, barAreaTop, x, barAreaTop + (barHeight + barSpacing) * barCount, GridPaint);
        }

        // Use Zip to enumerate values and labels together
        int i = 0;
        foreach (var (value, label) in _values.Zip(_labels))
        {
            float y = barAreaTop + i * (barHeight + barSpacing);
            float clampedValue = Math.Clamp(value, 0f, 100f);

            // Draw bar background
            var barBgRect = new SKRect(barStartX, y, barStartX + barWidth, y + barHeight);
            canvas.DrawRect(barBgRect, _barBackgroundPaint);

            // Draw bar foreground (green)
            float fillWidth = barWidth * (clampedValue / 100f);
            if (fillWidth > 0)
            {
                var barRect = new SKRect(barStartX, y, barStartX + fillWidth, y + barHeight);
                canvas.DrawRect(barRect, _barPaint);
            }

            // Draw label
            canvas.DrawText(label, bounds.Left + 20, y + barHeight / 2 + 5, TextFont, TextPaint);

            // Draw percentage
            string percentage = $"{clampedValue:F1}%";
            canvas.DrawText(percentage, barStartX + barWidth + 10, y + barHeight / 2 + 5, TextFont, TextPaint);

            i++;
        }
    }

    public override void Dispose()
    {
        _barPaint.Dispose();
        _barBackgroundPaint.Dispose();
        base.Dispose();
    }
}
