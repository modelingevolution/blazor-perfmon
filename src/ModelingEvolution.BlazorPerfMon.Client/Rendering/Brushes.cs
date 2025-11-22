using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Static collection of reusable SKPaint brushes to avoid allocation in hot rendering paths.
/// All brushes are thread-safe for read-only access.
/// WARNING: Do not modify brush properties during rendering.
/// </summary>
internal static class Brushes
{
    // Solid fill brushes
    public static readonly SKPaint White = new()
    {
        Color = ChartColors.White,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public static readonly SKPaint Green = new()
    {
        Color = ChartColors.Green,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public static readonly SKPaint Orange = new()
    {
        Color = ChartColors.Orange,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public static readonly SKPaint Blue = new()
    {
        Color = ChartColors.Blue,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public static readonly SKPaint BarBackground = new()
    {
        Color = ChartColors.BarBackground,
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    // Stroke brushes
    public static readonly SKPaint GreenStroke = new()
    {
        Color = ChartColors.Green,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        IsAntialias = true
    };

    public static readonly SKPaint OrangeStroke = new()
    {
        Color = ChartColors.Orange,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        IsAntialias = true
    };

    public static readonly SKPaint BlueStroke = new()
    {
        Color = ChartColors.Blue,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        IsAntialias = true
    };

    public static readonly SKPaint LightRedStroke = new()
    {
        Color = ChartColors.LightRed,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 5f,
        IsAntialias = true
    };

    public static readonly SKPaint GridStroke = new()
    {
        Color = ChartColors.GridLines,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3f,
        IsAntialias = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
    };

    public static readonly SKPaint OrangeDashedStroke = new()
    {
        Color = ChartColors.Orange,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 3f,
        IsAntialias = true,
        PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
    };

    public static readonly SKPaint AxisStroke = new()
    {
        Color = new SKColor(150, 150, 150),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        IsAntialias = false
    };

    // Text brushes with different sizes
    public static readonly SKPaint Text14 = new()
    {
        Color = ChartColors.White,
        TextSize = 14,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Normal)
    };

    public static readonly SKPaint Text16 = new()
    {
        Color = ChartColors.White,
        TextSize = 16,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Normal)
    };

    public static readonly SKPaint Text18Bold = new()
    {
        Color = ChartColors.White,
        TextSize = 18,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Bold)
    };

    public static readonly SKPaint TextBlack18Bold = new()
    {
        Color = SKColors.Black,
        TextSize = 18,
        IsAntialias = true,
        Typeface = SKTypeface.FromFamilyName("monospace", SKFontStyle.Bold)
    };

    public static readonly SKPaint TextGray14Center = new()
    {
        Color = SKColors.Gray,
        TextSize = 14,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center
    };
}
