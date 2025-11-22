using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Client.Models;
using SkiaSharp;

namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Interface for all chart types in the performance monitor.
/// Charts render at (0,0) and are positioned by the caller via canvas transforms.
/// </summary>
public interface IChart : IDisposable
{
    /// <summary>
    /// Updates the chart's data accessors with a new buffer snapshot.
    /// Should be called once per frame before rendering.
    /// </summary>
    void UpdateBuffer(ImmutableCircularBuffer<MetricSample> buffer);

    /// <summary>
    /// Renders the chart at the current canvas origin (0,0) with the specified size.
    /// Caller is responsible for canvas transforms (translation, clipping, etc.).
    /// </summary>
    /// <param name="canvas">The SKCanvas to render on (already translated to chart position)</param>
    /// <param name="size">The size to render the chart at</param>
    void Render(SKCanvas canvas, in SKSize size);
}
