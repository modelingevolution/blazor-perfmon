using ModelingEvolution.BlazorPerfMon.Client.Collections;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Renders horizontal bar chart.
/// </summary>
public sealed class BarChart : ChartBase
{
    private string _title = "Bar Chart";
    private IEnumerable<string> _labels = Enumerable.Empty<string>();
    private int _labelCount = 0;
    private IEnumerable<float> _values = Enumerable.Empty<float>();
    private int _valueCount = 0;
    private float _minScale;
    private float _maxScale = 100f;
    private string _units = "%";
    private string _valueFormat = "{0:F1}";
    private Func<float, SKPaint> _colorMapper = _ => ChartStyles.BarFill;
    private bool _enableMinMaxTracking = false;

    // Track min/max values per label for dotted lines
    private readonly Dictionary<string, (float min, float max)> _minMaxTracking = new();

    /// <summary>
    /// Sets the data for the bar chart.
    /// Accepts IEnumerable for zero-copy architecture.
    /// </summary>
    /// <param name="title">Chart title</param>
    /// <param name="labels">Labels for each bar</param>
    /// <param name="labelCount">Number of labels</param>
    /// <param name="values">Values for each bar</param>
    /// <param name="valueCount">Number of values</param>
    /// <param name="minScale">Minimum value for scale (default 0)</param>
    /// <param name="maxScale">Maximum value for scale (default 100)</param>
    /// <param name="units">Display units (default "%")</param>
    /// <param name="valueFormat">Format string for values (default "{0:F1}")</param>
    /// <param name="colorMapper">Optional function to map values to paint colors</param>
    /// <param name="enableMinMaxTracking">Enable min/max tracking with dotted lines (default false)</param>
    public void SetData(
        string title,
        IEnumerable<string> labels,
        int labelCount,
        IEnumerable<float> values,
        int valueCount,
        float minScale = 0f,
        float maxScale = 100f,
        string units = "%",
        string valueFormat = "{0:F1}",
        Func<float, SKPaint>? colorMapper = null,
        bool enableMinMaxTracking = false)
    {
        _title = title;
        _labels = labels;
        _labelCount = labelCount;
        _values = values;
        _valueCount = valueCount;
        _minScale = minScale;
        _maxScale = maxScale;
        _units = units;
        _valueFormat = valueFormat;
        _colorMapper = colorMapper ?? (_ => ChartStyles.BarFill);
        _enableMinMaxTracking = enableMinMaxTracking;
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

        // Use zero-allocation Zip to enumerate values and labels together
        int i = 0;
        foreach (var (value, label) in _values.ZipValues(_labels))
        {
            float y = barAreaTop + i * (barHeight + barSpacing);
            float clampedValue = Math.Clamp(value, _minScale, _maxScale);
            float fillRatio = (clampedValue - _minScale) / (_maxScale - _minScale);

            // Update min/max tracking if enabled
            if (_enableMinMaxTracking)
            {
                if (!_minMaxTracking.ContainsKey(label))
                {
                    _minMaxTracking[label] = (clampedValue, clampedValue);
                }
                else
                {
                    var (currentMin, currentMax) = _minMaxTracking[label];
                    _minMaxTracking[label] = (
                        Math.Min(currentMin, clampedValue),
                        Math.Max(currentMax, clampedValue)
                    );
                }
            }

            // Draw bar background
            var barBgRect = new SKRect(barStartX, y, barStartX + barWidth, y + barHeight);
            canvas.DrawRect(barBgRect, ChartStyles.BarBackground);

            // Draw bar foreground with color from mapper
            float fillWidth = barWidth * fillRatio;
            if (fillWidth > 0)
            {
                var barRect = new SKRect(barStartX, y, barStartX + fillWidth, y + barHeight);
                canvas.DrawRect(barRect, _colorMapper(value));
            }

            // Draw min/max dotted lines if tracking enabled
            if (_enableMinMaxTracking && _minMaxTracking.ContainsKey(label))
            {
                var (minValue, maxValue) = _minMaxTracking[label];

                // Draw min line (blue dotted)
                float minRatio = (minValue - _minScale) / (_maxScale - _minScale);
                float minX = barStartX + (barWidth * minRatio);
                canvas.DrawLine(minX, y, minX, y + barHeight, ChartStyles.DottedLine);

                // Draw max line (orange dotted)
                float maxRatio = (maxValue - _minScale) / (_maxScale - _minScale);
                float maxX = barStartX + (barWidth * maxRatio);
                canvas.DrawLine(maxX, y, maxX, y + barHeight, ChartStyles.DottedLineOrange);

                // Draw min value label (left aligned, black text)
                string minText = string.Format(_valueFormat, minValue);
                canvas.DrawText(minText, barStartX + 5, y + barHeight / 2 + 6, ChartStyles.LabelBlackBold);

                // Draw max value label (right aligned, white text)
                string maxText = string.Format(_valueFormat, maxValue);
                var maxTextBounds = new SKRect();
                ChartStyles.LabelBold.MeasureText(maxText, ref maxTextBounds);
                float maxTextX = barStartX + barWidth - maxTextBounds.Width - 5;
                canvas.DrawText(maxText, maxTextX, y + barHeight / 2 + 6, ChartStyles.LabelBold);
            }

            // Draw label
            canvas.DrawText(label, bounds.Left + 20, y + barHeight / 2 + 5, TextFont, TextPaint);

            // Draw value with units
            string valueText = string.Format(_valueFormat, clampedValue) + _units;
            canvas.DrawText(valueText, barStartX + barWidth + 10, y + barHeight / 2 + 5, TextFont, TextPaint);

            i++;
        }
    }

    public override void Dispose()
    {
        // Uses shared static paints from ChartStyles - no disposal needed
        base.Dispose();
    }
}
