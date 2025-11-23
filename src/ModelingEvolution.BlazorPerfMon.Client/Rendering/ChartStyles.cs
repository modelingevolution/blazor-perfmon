using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Unified chart styling to ensure consistent appearance across all charts.
/// All text paints, fonts, and visual styles are centralized here.
/// </summary>
internal static class ChartStyles
{
    // ===== Text Styles =====

    /// <summary>
    /// Title text paint - 24pt bold monospace white.
    /// Use for all chart titles.
    /// </summary>
    public static readonly SKPaint Title = new()
    {
        Color = ChartColors.White,
        TextSize = 24,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Bold)
    };

    /// <summary>
    /// Title font (SKFont version) - 24pt.
    /// Use for canvas.DrawText(text, x, y, font, paint) calls.
    /// </summary>
    public static readonly SKFont TitleFont = new()
    {
        Size = 24f
    };

    /// <summary>
    /// Label text paint - 16pt normal monospace white.
    /// Use for bar labels, value labels, and general chart text.
    /// </summary>
    public static readonly SKPaint Label = new()
    {
        Color = ChartColors.White,
        TextSize = 16,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Normal)
    };

    /// <summary>
    /// Label font (SKFont version) - 16pt.
    /// Use for canvas.DrawText(text, x, y, font, paint) calls.
    /// </summary>
    public static readonly SKFont LabelFont = new()
    {
        Size = 16f
    };

    /// <summary>
    /// Label text paint (bold variant) - 16pt bold monospace white.
    /// Use for emphasized labels or in-chart data labels.
    /// </summary>
    public static readonly SKPaint LabelBold = new()
    {
        Color = ChartColors.White,
        TextSize = 16,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Bold)
    };

    /// <summary>
    /// Label text paint (black variant) - 16pt bold monospace black.
    /// Use for labels on light/colored backgrounds for better contrast.
    /// </summary>
    public static readonly SKPaint LabelBlackBold = new()
    {
        Color = SKColors.Black,
        TextSize = 16,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Bold)
    };

    /// <summary>
    /// Axis label text paint - 14pt normal monospace white.
    /// Use for axis labels, time labels, and small supplementary text.
    /// </summary>
    public static readonly SKPaint AxisLabel = new()
    {
        Color = ChartColors.White,
        TextSize = 14,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Normal)
    };

    /// <summary>
    /// Placeholder/empty state text paint - 14pt centered gray.
    /// Use for "No data" messages and empty states.
    /// </summary>
    public static readonly SKPaint Placeholder = new()
    {
        Color = SKColors.Gray,
        TextSize = 14,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Normal)
    };

    /// <summary>
    /// Generic text paint for use with SKFont.
    /// White color, antialiased.
    /// </summary>
    public static readonly SKPaint Text = new()
    {
        Color = SKColors.White,
        IsAntialias = true
    };

    // ===== Background & Structure Styles =====

    /// <summary>
    /// Chart background fill paint - dark gray (#1a1a1a).
    /// </summary>
    public static readonly SKPaint Background = new()
    {
        Color = new SKColor(26, 26, 26),
        Style = SKPaintStyle.Fill,
        IsAntialias = false
    };

    /// <summary>
    /// Grid line paint - medium gray, 1px stroke.
    /// Use for chart grid lines.
    /// </summary>
    public static readonly SKPaint Grid = new()
    {
        Color = new SKColor(70, 70, 70),
        StrokeWidth = 1f,
        IsAntialias = false,
        Style = SKPaintStyle.Stroke
    };

    /// <summary>
    /// Axis line paint - light gray, 2px stroke.
    /// Use for X and Y axes.
    /// </summary>
    public static readonly SKPaint Axis = new()
    {
        Color = new SKColor(150, 150, 150),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        IsAntialias = false
    };

    // ===== Bar Chart Styles =====

    /// <summary>
    /// Bar background fill paint - dark gray (#323232).
    /// Use for unfilled portion of progress bars.
    /// </summary>
    public static readonly SKPaint BarBackground = new()
    {
        Color = new SKColor(50, 50, 50),
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Bar foreground fill paint - green.
    /// Use for filled portion of progress bars (CPU, memory, etc.).
    /// </summary>
    public static readonly SKPaint BarFill = new()
    {
        Color = ChartColors.Green,
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    // ===== Temperature Bar Styles =====

    /// <summary>
    /// Temperature bar paint - cool zone (below 50°C) - blue.
    /// </summary>
    public static readonly SKPaint TemperatureCool = new()
    {
        Color = new SKColor(100, 200, 255),
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Temperature bar paint - normal zone (50-60°C) - green.
    /// </summary>
    public static readonly SKPaint TemperatureNormal = new()
    {
        Color = ChartColors.Green,
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Temperature bar paint - warm zone (60-70°C) - yellow.
    /// </summary>
    public static readonly SKPaint TemperatureWarm = new()
    {
        Color = new SKColor(255, 255, 100),
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Temperature bar paint - hot zone (70-85°C) - orange.
    /// </summary>
    public static readonly SKPaint TemperatureHot = new()
    {
        Color = ChartColors.Orange,
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Temperature bar paint - critical zone (above 85°C) - red.
    /// </summary>
    public static readonly SKPaint TemperatureCritical = new()
    {
        Color = ChartColors.LightRed,
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    /// <summary>
    /// Maps temperature value to appropriate color paint.
    /// </summary>
    /// <param name="tempCelsius">Temperature in Celsius</param>
    /// <returns>SKPaint with appropriate color for the temperature zone</returns>
    public static SKPaint GetTemperaturePaint(float tempCelsius)
    {
        return tempCelsius switch
        {
            < 50f => TemperatureCool,
            < 60f => TemperatureNormal,
            < 70f => TemperatureWarm,
            < 85f => TemperatureHot,
            _ => TemperatureCritical
        };
    }

    // ===== Line Chart Styles =====

    /// <summary>
    /// Creates a line stroke paint with the specified color.
    /// Use for time series line charts.
    /// </summary>
    public static SKPaint CreateLinePaint(SKColor color) => new()
    {
        Color = color,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f
    };

    /// <summary>
    /// Creates a fill paint with the specified color and alpha.
    /// Use for area fills under time series lines.
    /// </summary>
    public static SKPaint CreateFillPaint(SKColor color, byte alpha = 40) => new()
    {
        Color = new SKColor(color.Red, color.Green, color.Blue, alpha),
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    // ===== Indicator Line Styles =====

    /// <summary>
    /// Dotted line paint - gray, 3px stroke.
    /// Use for min/max indicators.
    /// </summary>
    public static readonly SKPaint DottedLine = new()
    {
        Color = ChartColors.GridLines,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3f,
        IsAntialias = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
    };

    /// <summary>
    /// Dotted line paint - orange, 3px stroke.
    /// Use for max value indicators.
    /// </summary>
    public static readonly SKPaint DottedLineOrange = new()
    {
        Color = ChartColors.Orange,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3f,
        IsAntialias = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
    };

    /// <summary>
    /// Solid indicator line paint - light red, 5px stroke.
    /// Use for current value indicators (e.g., CPU% vertical line).
    /// </summary>
    public static readonly SKPaint IndicatorLine = new()
    {
        Color = ChartColors.LightRed,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 5f,
        IsAntialias = true
    };
}
