using SkiaSharp;

namespace Frontend.Rendering;

/// <summary>
/// Renders time-series line chart with time axis.
/// </summary>
public sealed class TimeSeriesChart : ChartBase
{
    private readonly SKPaint _axisPaint;
    private readonly List<(string Label, IEnumerable<float> Data, int Count, SKColor Color)> _dataSeries = new();
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
    /// Sets the data for the time series chart (single series).
    /// </summary>
    public void SetData(string title, IEnumerable<float> dataPoints, int count, int maxDataPoints, int collectionIntervalMs = 500)
    {
        _title = title;
        _maxDataPoints = maxDataPoints;
        _collectionIntervalMs = collectionIntervalMs;
        _dataSeries.Clear();
        _dataSeries.Add((title, dataPoints, count, new SKColor(100, 255, 100))); // Green
        _useDynamicScale = false;
        _minValue = 0f;
        _maxValue = 100f;
    }

    /// <summary>
    /// Sets the data for the time series chart with multiple series and dynamic scaling.
    /// </summary>
    public void SetMultiSeriesData(string title, (string Label, IEnumerable<float> Data, int Count, SKColor Color)[] series, int maxDataPoints, int collectionIntervalMs, bool useDynamicScale = false)
    {
        _title = title;
        _maxDataPoints = maxDataPoints;
        _collectionIntervalMs = collectionIntervalMs;
        _timeWindowMs = maxDataPoints * collectionIntervalMs;
        _dataSeries.Clear();
        _dataSeries.AddRange(series);
        _useDynamicScale = useDynamicScale;
        _timestamps = Enumerable.Empty<uint>();
        _timestampCount = 0;

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
    /// Sets the data with timestamps for precise time-based rendering.
    /// Accepts IEnumerable for zero-copy architecture.
    /// </summary>
    public void SetMultiSeriesDataWithTimestamps(string title, (string Label, IEnumerable<float> Data, int Count, SKColor Color)[] series, IEnumerable<uint> timestamps, int timestampCount, int timeWindowMs, bool useDynamicScale = false)
    {
        _title = title;
        _dataSeries.Clear();
        _dataSeries.AddRange(series);
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
        if (_dataSeries.Count == 0)
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
        if (_dataSeries.Count == 0 || _dataSeries[0].Count == 0)
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

        // Use timestamp-based rendering if timestamps are available
        bool useTimestamps = _timestampCount > 0;

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

            int dataLength = Math.Min(series.Count, useTimestamps ? _timestampCount : series.Count);
            if (dataLength < 2) continue;

            // Calculate X positions based on timestamps or evenly-spaced fallback
            if (useTimestamps && _timestampCount > 0)
            {
                // Timestamp-based rendering: right edge = current time (not latest data!)
                uint currentTimestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                bool firstPoint = true;
                float prevX = 0, prevY = 0;
                bool prevValid = false;

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

                    // Handle left-edge clipping with interpolation
                    if (x < bounds.Left)
                    {
                        // Point is past the left edge
                        if (prevValid && prevX >= bounds.Left)
                        {
                            // Previous point was visible, interpolate at left boundary
                            float t = (bounds.Left - prevX) / (x - prevX);
                            float interpY = prevY + t * (y - prevY);

#if DEBUG
                            Console.WriteLine($"[TimeSeriesChart] Interpolating at left edge: prevX={prevX:F1}, x={x:F1}, prevY={prevY:F1}, y={y:F1}, t={t:F3}, interpY={interpY:F1}, bounds.Left={bounds.Left:F1}");
#endif

                            if (firstPoint)
                            {
                                fillPath.MoveTo(bounds.Left, bounds.Bottom);
                                fillPath.LineTo(bounds.Left, interpY);
                                path.MoveTo(bounds.Left, interpY);
                                firstPoint = false;
#if DEBUG
                                Console.WriteLine($"[TimeSeriesChart] First point interpolated at left edge: ({bounds.Left:F1}, {interpY:F1})");
#endif
                            }
                            else
                            {
                                path.LineTo(bounds.Left, interpY);
                                fillPath.LineTo(bounds.Left, interpY);
#if DEBUG
                                Console.WriteLine($"[TimeSeriesChart] Continuing path to interpolated point at left edge: ({bounds.Left:F1}, {interpY:F1})");
#endif
                            }
                        }
                        // Update previous and continue (don't render this point)
                        prevX = x;
                        prevY = y;
                        prevValid = true;
                        continue;
                    }

                    // Point is visible
                    if (firstPoint)
                    {
                        // Check if we need to interpolate at left edge first
                        if (prevValid && prevX < bounds.Left)
                        {
                            // Interpolate entry point at left boundary
                            float t = (bounds.Left - prevX) / (x - prevX);
                            float interpY = prevY + t * (y - prevY);
#if DEBUG
                            Console.WriteLine($"[TimeSeriesChart] Entry interpolation from left: prevX={prevX:F1}, x={x:F1}, prevY={prevY:F1}, y={y:F1}, t={t:F3}, interpY={interpY:F1}");
#endif
                            fillPath.MoveTo(bounds.Left, bounds.Bottom);
                            fillPath.LineTo(bounds.Left, interpY);
                            path.MoveTo(bounds.Left, interpY);
                            // Draw to current point after interpolation
                            path.LineTo(x, y);
                            fillPath.LineTo(x, y);
                        }
                        else
                        {
                            fillPath.MoveTo(x, bounds.Bottom);
                            fillPath.LineTo(x, y);
                            path.MoveTo(x, y);
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

                if (!firstPoint)
                {
                    // Complete fill path back to bottom-right
                    // Calculate where the last point should be relative to current time
                    uint lastDataTimestamp = _timestamps.Last();
                    long lastTimeDelta = (long)currentTimestamp - (long)lastDataTimestamp;
                    float lastTimeRatio = (float)lastTimeDelta / _timeWindowMs;
                    float lastX = bounds.Right - (lastTimeRatio * bounds.Width);
                    fillPath.LineTo(lastX, bounds.Bottom);
                    fillPath.Close();
                }
            }
            else
            {
                // Fallback: evenly-spaced rendering (old behavior)
                float xStep = bounds.Width / (dataLength - 1);
                fillPath.MoveTo(bounds.Left, bounds.Bottom);

                bool firstPoint = true;
                int i = 0;
                foreach (var value in series.Data)
                {
                    float normalizedValue = (value - _minValue) / valueRange;
                    normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

                    float x = bounds.Left + (i * xStep);
                    float y = bounds.Bottom - (bounds.Height * normalizedValue);

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

                    i++;
                }

                fillPath.LineTo(bounds.Left + ((dataLength - 1) * xStep), bounds.Bottom);
                fillPath.Close();
            }

            // Draw fill first, then line on top
            canvas.DrawPath(fillPath, fillPaint);
            canvas.DrawPath(path, linePaint);
        }
    }

    private void DrawTimeLabels(SKCanvas canvas, SKRect bounds)
    {
        // Time window in seconds (samples * interval in ms / 1000)
        float totalSeconds = _maxDataPoints * (_collectionIntervalMs / 1000f);

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
