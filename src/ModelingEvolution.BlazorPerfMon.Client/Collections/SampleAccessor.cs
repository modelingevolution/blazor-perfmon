using System.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace Frontend.Collections;

/// <summary>
/// Zero-copy adapter/facade over ImmutableCircularBuffer with projection selector.
/// Provides IEnumerable<T> without allocating arrays - selector is applied during enumeration.
/// Can be updated with a new buffer reference for time-consistent rendering.
/// </summary>
public sealed class SampleAccessor<T> : IEnumerable<T>
{
    private ImmutableCircularBuffer<MetricSample> _samples;
    private readonly Func<MetricSample, T> _selector;

    public SampleAccessor(
        ImmutableCircularBuffer<MetricSample> samples,
        Func<MetricSample, T> selector)
    {
        _samples = samples;
        _selector = selector;
    }

    /// <summary>
    /// Updates the buffer reference for time-consistent rendering.
    /// All accessors should be updated with the same buffer snapshot before rendering.
    /// </summary>
    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> samples)
    {
        _samples = samples;
    }

    /// <summary>
    /// Gets the number of samples in the buffer.
    /// </summary>
    public int Count => _samples.Count;

    /// <summary>
    /// Enumerates the projected values without allocating arrays.
    /// The selector is applied on-the-fly during enumeration.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var sample in _samples)
        {
            yield return _selector(sample);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
