using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Centralized color theme for all charts to maintain visual consistency
/// </summary>
public static class ChartColors
{
    // Primary colors for data visualization
    public static readonly SKColor Green = new SKColor(100, 255, 100);         // CPU bars, Network RX, Disk Read
    public static readonly SKColor LightRed = new SKColor(255, 100, 100);      // Disk Write, Network TX, Docker CPU line
    public static readonly SKColor Blue = new SKColor(100, 200, 255);          // RAM, general lines
    public static readonly SKColor Orange = new SKColor(255, 200, 100);        // GPU

    // Background colors
    public static readonly SKColor DarkBackground = new SKColor(26, 26, 26);   // #1a1a1a - chart background
    public static readonly SKColor BarBackground = new SKColor(50, 50, 50);    // Bar/gauge backgrounds

    // UI elements
    public static readonly SKColor GridLines = new SKColor(70, 70, 70);        // Grid lines
    public static readonly SKColor Text = new SKColor(150, 150, 150);          // Axis labels, text
    public static readonly SKColor White = SKColors.White;                     // Bright text, labels

    // Semi-transparent variants
    public static SKColor WithAlpha(this SKColor color, byte alpha)
    {
        return new SKColor(color.Red, color.Green, color.Blue, alpha);
    }
}
