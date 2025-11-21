using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Renders time-series line chart with time axis.
/// </summary>
public sealed class TimeSeriesChart : ChartBase
{
    private readonly SKPaint _axisPaint;
    private TimeSeriesF[] _dataSeries = Array.Empty<TimeSeriesF>();
    private IEnumerable<uint> _timestamps = Enumerable.Empty<uint>();
    private int _timestampCount = 0;

    private string _title = "Time Series";
    private int _maxDataPoints = 120;
    private int _collectionIntervalMs = 500;
    private int _timeWindowMs = 60000; // 60 seconds default
    private float _minValue = 0f;
    private float _maxValue = 100f;
    private bool _useDynamicScale = false;

    public TimeSeriesChart()
    {
        _axisPaint = new SKPaint
        {
            Color = new SKColor(150, 150, 150),
            StrokeWidth = 2f,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke
        };
    }

    /// <summary>
    /// Sets up the chart with title, series data, and timestamps for precise time-based rendering.
    /// Accepts IEnumerable for zero-copy architecture.
    /// </summary>
    public void Setup(string title, TimeSeriesF[] series, IEnumerable<uint> timestamps, int timestampCount, int timeWindowMs, bool useDynamicScale = false)
    {
        _title = title;
        _dataSeries = series;
        _timestamps = timestamps;
        _timestampCount = timestampCount;
        _timeWindowMs = timeWindowMs;
        _maxDataPoints = timestampCount;
        _useDynamicScale = useDynamicScale;

        if (_useDynamicScale)
        {
            CalculateDynamicScale();
        }
        else
        {
            _minValue = 0f;
            _maxValue = 100f;
        }
    }

    /// <summary>
    /// Recalculate min/max for dynamic scaling based on current data.
    /// </summary>
    public void UpdateDynamicScale()
    {
        if (_useDynamicScale)
        {
            CalculateDynamicScale();
        }
    }

    private void CalculateDynamicScale()
    {
        if (_dataSeries.Length == 0)
        {
            _minValue = 0f;
            _maxValue = 100f;
            return;
        }

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var series in _dataSeries)
        {
            foreach (var value in series.Data)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        // Add 10% padding
        float range = max - min;
        if (range < 0.01f) range = 1f; // Minimum range

        _minValue = Math.Max(0, min - range * 0.1f);
        _maxValue = max + range * 0.1f;
    }

    protected override void RenderContent(SKCanvas canvas)
    {
        if (_dataSeries.Length == 0 || _dataSeries[0].Count == 0)
            return;

        var bounds = Bounds;

        // Title
        canvas.DrawText(_title, bounds.Left + 20, bounds.Top + 30, TitleFont, TextPaint);

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

        // Draw data lines for all series
        DrawDataLines(canvas, graphBounds);

        // Draw time labels
        DrawTimeLabels(canvas, graphBounds);

        // Draw value labels
        DrawValueLabels(canvas, graphBounds);
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
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, GridPaint);
        }

        // Vertical grid lines (every 15 seconds for 60 second window)
        for (int i = 1; i < 4; i++)
        {
            float x = bounds.Left + (bounds.Width * i / 4f);
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, GridPaint);
        }
    }

    private void DrawDataLines(SKCanvas canvas, SKRect bounds)
    {
        float valueRange = _maxValue - _minValue;
        if (valueRange < 0.01f) valueRange = 1f;

        // Timestamp-based rendering: right edge = current time (not latest data!)
        uint currentTimestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var series in _dataSeries)
        {
            if (series.Count < 2)
                continue;

            using var linePaint = new SKPaint
            {
                Color = series.Color,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f
            };

            using var fillPaint = new SKPaint
            {
                Color = new SKColor(series.Color.Red, series.Color.Green, series.Color.Blue, 40),
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var path = new SKPath();
            using var fillPath = new SKPath();

            bool firstPoint = true;
            float prevX = 0, prevY = 0;
            bool prevValid = false;

            // Calculate culling threshold: skip samples way outside the left edge
            // Keep a small margin for smooth interpolation
            float cullThreshold = bounds.Left - (bounds.Width * 0.05f);
            float rightMargin = bounds.Right + (bounds.Width * 0.05f);

            // Zip data with timestamps for correlated enumeration
            foreach (var (value, timestamp) in series.Data.Zip(_timestamps))
            {
                float normalizedValue = (value - _minValue) / valueRange;
                normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

                // Calculate X position based on timestamp delta from CURRENT time
                long timeDelta = (long)currentTimestamp - (long)timestamp;

                // Position: right edge - (time delta / time window) * width
                float timeRatio = (float)timeDelta / _timeWindowMs;
                float x = bounds.Right - (timeRatio * bounds.Width);
                float y = bounds.Bottom - (bounds.Height * normalizedValue);

                // Skip samples that are way too far left (optimization + numerical stability)
                if (x < cullThreshold)
                {
                    // Update previous and skip
                    prevX = x;
                    prevY = y;
                    prevValid = true;
                    continue;
                }

                // Handle left-edge clipping with interpolation
                if (x < bounds.Left)
                {
                    // Point is just outside the left edge (within margin)
                    // Keep for interpolation
                    prevX = x;
                    prevY = y;
                    prevValid = true;
                    continue;
                }

                // Check if we've gone past the right edge
                if (x > bounds.Right)
                {
                    // Interpolate at right boundary if we have a previous visible point
                    if (!firstPoint && prevValid && prevX <= bounds.Right)
                    {
                        float t = (bounds.Right - prevX) / (x - prevX);
                        float interpY = prevY + t * (y - prevY);
#if DEBUG
                        Console.WriteLine($"[{series.Label}] Right edge interpolation: prevX={prevX:F1} x={x:F1} t={t:F3} interpY={interpY:F1}");
#endif
                        path.LineTo(bounds.Right, interpY);
                        fillPath.LineTo(bounds.Right, interpY);

                        // Complete fill and break
                        fillPath.LineTo(bounds.Right, bounds.Bottom);
                        fillPath.Close();
                        canvas.DrawPath(fillPath, fillPaint);
                        canvas.DrawPath(path, linePaint);
                        goto NextSeries; // Break to outer foreach
                    }
                    // No interpolation needed, complete what we have
                    if (!firstPoint)
                    {
                        fillPath.LineTo(prevX, bounds.Bottom);
                        fillPath.Close();
                        canvas.DrawPath(fillPath, fillPaint);
                        canvas.DrawPath(path, linePaint);
                        goto NextSeries;
                    }
                    break;
                }

                // Point is visible (bounds.Left <= x <= bounds.Right)
                if (firstPoint)
                {
                    // Check if we need to interpolate at left edge first
                    if (prevValid && prevX < bounds.Left)
                    {
                        // Interpolate entry point at left boundary
                        float t = (bounds.Left - prevX) / (x - prevX);
                        float interpY = prevY + t * (y - prevY);
#if DEBUG
                        Console.WriteLine($"[{series.Label}] Left edge interpolation: prevX={prevX:F1} x={x:F1} t={t:F3} interpY={interpY:F1}");
#endif
                        // Start path at left edge with interpolated value
                        fillPath.MoveTo(bounds.Left, bounds.Bottom);
                        fillPath.LineTo(bounds.Left, interpY);
                        path.MoveTo(bounds.Left, interpY);
                        // Draw to current visible point
                        path.LineTo(x, y);
                        fillPath.LineTo(x, y);
                    }
                    else
                    {
                        // No previous point or previous was also visible
                        // Start directly at current point
                        fillPath.MoveTo(x, bounds.Bottom);
                        fillPath.LineTo(x, y);
                        path.MoveTo(x, y);
#if DEBUG
                        Console.WriteLine($"[{series.Label}] First visible point (no interpolation): x={x:F1} y={y:F1}");
#endif
                    }
                    firstPoint = false;
                }
                else
                {
                    path.LineTo(x, y);
                    fillPath.LineTo(x, y);
                }

                prevX = x;
                prevY = y;
                prevValid = true;
            }

            // Only reached if we didn't break early (all samples are within or before visible area)
            if (!firstPoint)
            {
                // Extend to right edge if last data point is before current time
                if (prevValid && prevX < bounds.Right)
                {
                    path.LineTo(bounds.Right, prevY);
                    fillPath.LineTo(bounds.Right, prevY);
#if DEBUG
                    Console.WriteLine($"[{series.Label}] Extending to right edge: ({bounds.Right:F1}, {prevY:F1})");
#endif
                }

                // Complete fill path back to bottom
                fillPath.LineTo(prevX < bounds.Right ? bounds.Right : prevX, bounds.Bottom);
                fillPath.Close();
            }

            // Draw fill first, then line on top
            canvas.DrawPath(fillPath, fillPaint);
            canvas.DrawPath(path, linePaint);

            NextSeries:; // Label for early exit from right edge interpolation
        }
    }

    private void DrawTimeLabels(SKCanvas canvas, SKRect bounds)
    {
        // Use the actual time window (matches rendering calculation)
        float totalSeconds = _timeWindowMs / 1000f;

        // Draw labels at 0s, 25%, 50%, 75%, 100%
        for (int i = 0; i <= 4; i++)
        {
            float seconds = totalSeconds * i / 4f;
            float x = bounds.Left + (bounds.Width * i / 4f);
            string label = $"-{totalSeconds - seconds:F0}s";

            canvas.DrawText(label, x - 15, bounds.Bottom + 20, TextFont, TextPaint);
        }
    }

    private void DrawValueLabels(SKCanvas canvas, SKRect bounds)
    {
        // Draw labels at min, 25%, 50%, 75%, max
        for (int i = 0; i <= 4; i++)
        {
            float fraction = i / 4f;
            float value = _minValue + (_maxValue - _minValue) * fraction;
            float y = bounds.Bottom - (bounds.Height * i / 4f);

            // Format based on magnitude
            string label;
            if (value >= 1000)
                label = $"{value / 1000f:F1}K";
            else if (value >= 1)
                label = $"{value:F1}";
            else
                label = $"{value:F2}";

            canvas.DrawText(label, bounds.Left - 45, y + 5, TextFont, TextPaint);
        }
    }

    public override void Dispose()
    {
        _axisPaint.Dispose();
        base.Dispose();
    }
}
