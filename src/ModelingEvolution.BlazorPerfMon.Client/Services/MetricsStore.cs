using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Client.Services;

/// <summary>
/// Thread-safe metrics store using immutable circular buffer.
/// Uses volatile reference for lock-free reads.
/// </summary>
public sealed class MetricsStore
{
    private volatile ImmutableCircularBuffer<MetricSample> _buffer;

    /// <summary>
    /// Event fired when new metrics are added to the store.
    /// </summary>
    public event Action? OnMetricsUpdated;

    /// <summary>
    /// Creates a new MetricsStore with the specified number of intervals.
    /// Buffer capacity is intervals + 1 to properly represent the time range.
    /// Example: 120 intervals × 500ms = 60 seconds requires 121 sample points.
    /// </summary>
    /// <param name="intervals">The number of time intervals to store in the buffer</param>
    public MetricsStore(int intervals)
    {
        // Buffer needs intervals + 1 samples to represent the full time range
        _buffer = new ImmutableCircularBuffer<MetricSample>(intervals + 5);
    }

    /// <summary>
    /// Adds a new sample to the buffer. Thread-safe.
    /// </summary>
    /// <param name="sample">The metric sample to add</param>
    public void AddSample(MetricSample sample)
    {
        _buffer = _buffer.Add(sample);
        OnMetricsUpdated?.Invoke();
    }


    /// <summary>
    /// Gets the current buffer reference. Thread-safe due to volatile.
    /// </summary>
    public ImmutableCircularBuffer<MetricSample> GetBuffer() => _buffer;
}
