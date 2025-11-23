using ModelingEvolution;
using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Extensions;
using ModelingEvolution.BlazorPerfMon.Shared;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Chart for displaying Docker container metrics with double-bar visualization.
/// Left: Container name (max 10 chars)
/// Center: Memory usage bar with CPU% label in middle
/// Right: Memory value + CPU vertical line (normalized 0-100%)
/// Scale: max(all containers) + 10% OR system RAM (whichever is lower)
/// Max memory can only grow on UI
/// </summary>
internal sealed class DockerContainersChart : IChart
{
    private ulong _systemRam = 0;
    private ulong _maxMemoryBytes = 0;
    private ImmutableCircularBuffer<MetricSample> _buffer = null!;

    // Track min/max RAM per container for dotted lines
    private readonly Dictionary<string, (ulong min, ulong max)> _ramMinMax = new();

    public DockerContainersChart()
    {
    }

    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer)
    {
        _buffer = buffer;
    }

    public void Render(SKCanvas canvas, in SKSize size)
    {
        if (_buffer == null || _buffer.Count == 0)
            return;

        var latest = _buffer.Last();

        // Get system RAM from the latest sample (if not already set)
        if (_systemRam == 0 && latest.Ram.TotalBytes > 0)
            _systemRam = latest.Ram.TotalBytes;

        var containers = latest.DockerContainers;

        if (containers == null || containers.Length == 0)
        {
            DrawNoContainers(canvas, size);
            return;
        }

        // Draw title at the top
        const float titleHeight = 35f;
        var titleText = $"Docker containers: {containers.Length}";
        canvas.DrawText(titleText, 20, titleHeight - 5, ChartStyles.Title);

        // Calculate max memory: max(all containers) + 10% OR system RAM (whichever is lower)
        var currentMax = containers.Max(c => c.MemoryUsageBytes);
        var calculatedMax = (ulong)(currentMax * 1.1);
        var newMax = _systemRam > 0 ? Math.Min(calculatedMax, _systemRam) : calculatedMax;

        // Max memory can only grow, never shrink
        if (newMax > _maxMemoryBytes)
            _maxMemoryBytes = newMax;

        // If we still have no max (first sample with no data), use a default
        if (_maxMemoryBytes == 0)
            _maxMemoryBytes = 1024 * 1024 * 1024; // 1GB default

        // Calculate bar dimensions (same as CPU bars)
        const float barSpacing = 4f;
        const float barMaxHeight = 40f; // Same as CPU bars

        // Adjust available height for title
        float availableHeight = size.Height - titleHeight;
        float barHeight = Math.Min((availableHeight - 10) / containers.Length - barSpacing, barMaxHeight);

        // Calculate how many containers can fit fully visible
        int visibleContainers = (int)((availableHeight - 10) / (barHeight + barSpacing));
        visibleContainers = Math.Min(visibleContainers, containers.Length);

        // Render only fully visible containers (start below title)
        for (int i = 0; i < visibleContainers; i++)
        {
            var container = containers[i];
            var y = titleHeight + 5 + i * (barHeight + barSpacing);
            RenderContainer(canvas, container, y, barHeight, size.Width);
        }
    }

    private void RenderContainer(SKCanvas canvas, DockerContainerMetric container, float y, float barHeight, float width)
    {
        const float labelWidth = 100f;
        const float valueWidth = 120f;
        const float padding = 5f;

        // Truncate container name to 10 characters
        var displayName = container.Name.Length > 10 ? container.Name.Substring(0, 10) : container.Name;

        // Calculate dimensions
        var barX = labelWidth + padding;
        var barWidth = width - labelWidth - valueWidth - (padding * 3);

        // Update min/max RAM tracking for this container
        if (!_ramMinMax.ContainsKey(container.ContainerId))
        {
            _ramMinMax[container.ContainerId] = (container.MemoryUsageBytes, container.MemoryUsageBytes);
        }
        else
        {
            var (currentMin, currentMax) = _ramMinMax[container.ContainerId];
            _ramMinMax[container.ContainerId] = (
                Math.Min(currentMin, container.MemoryUsageBytes),
                Math.Max(currentMax, container.MemoryUsageBytes)
            );
        }

        var (minRam, maxRam) = _ramMinMax[container.ContainerId];

        // Draw container name (left) - 16pt label
        canvas.DrawText(displayName, padding, y + (barHeight / 2) + 6, ChartStyles.Label);

        // Draw memory bar background
        canvas.DrawRect(barX, y, barWidth, barHeight, ChartStyles.BarBackground);

        // Draw memory bar foreground (green - same as CPU bars)
        var memoryRatio = _maxMemoryBytes > 0 ? (float)container.MemoryUsageBytes / _maxMemoryBytes : 0f;
        var filledWidth = barWidth * memoryRatio;
        canvas.DrawRect(barX, y, filledWidth, barHeight, ChartStyles.BarFill);

        // Draw dotted horizontal lines for min/max RAM
        var minRamRatio = _maxMemoryBytes > 0 ? (float)minRam / _maxMemoryBytes : 0f;
        var minRamX = barX + (barWidth * minRamRatio);
        canvas.DrawLine(minRamX, y, minRamX, y + barHeight, ChartStyles.DottedLine);

        var maxRamRatio = _maxMemoryBytes > 0 ? (float)maxRam / _maxMemoryBytes : 0f;
        var maxRamX = barX + (barWidth * maxRamRatio);
        canvas.DrawLine(maxRamX, y, maxRamX, y + barHeight, ChartStyles.DottedLineOrange);

        // Draw CPU % label in the middle of the bar
        var cpuNormalized = Math.Min(container.CpuPercent, 100f);
        var cpuText = $"CPU: {cpuNormalized:F1}%";

        // Use black text when memory > 50% for better contrast on green memory bar background
        var cpuTextPaint = memoryRatio > 0.5f ? ChartStyles.LabelBlackBold : ChartStyles.LabelBold;

        // Measure text to center it
        var textBounds = new SKRect();
        cpuTextPaint.MeasureText(cpuText, ref textBounds);
        var textX = barX + (barWidth / 2) - (textBounds.Width / 2);
        var textY = y + (barHeight / 2) + (textBounds.Height / 2);
        canvas.DrawText(cpuText, textX, textY, cpuTextPaint);

        // CPU vertical line (light red, wider)
        var cpuRatio = cpuNormalized / 100f;
        var cpuLineX = barX + (barWidth * cpuRatio);
        canvas.DrawLine(cpuLineX, y, cpuLineX, y + barHeight, ChartStyles.IndicatorLine);

        // Draw memory value (right) - 16pt label
        Bytes memoryBytes = (long)container.MemoryUsageBytes;
        var memoryText = memoryBytes.FormatFixed();
        var valueX = barX + barWidth + padding;
        canvas.DrawText(memoryText, valueX, y + (barHeight / 2) + 6, ChartStyles.Label);
    }

    private void DrawNoContainers(SKCanvas canvas, SKSize size)
    {
        canvas.DrawText("No Docker containers running", size.Width / 2, size.Height / 2, ChartStyles.Placeholder);
    }

    public void Dispose()
    {
        // No disposable resources
    }
}
