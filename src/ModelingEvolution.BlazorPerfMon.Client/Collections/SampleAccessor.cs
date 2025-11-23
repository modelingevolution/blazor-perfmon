using System.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Zero-copy adapter/facade over ImmutableCircularBuffer with projection selector.
/// Provides IEnumerable without allocating arrays - selector is applied during enumeration.
/// Can be updated with a new buffer reference for time-consistent rendering.
/// </summary>
/// <typeparam name="T">The type of projected values returned by the selector.</typeparam>
internal sealed class SampleAccessor<T> : IEnumerable<T>
{
    private ImmutableCircularBuffer<MetricSample> _samples;
    private readonly Func<MetricSample, T> _selector;

    /// <summary>
    /// Initializes a new instance of the SampleAccessor class.
    /// </summary>
    /// <param name="samples">The initial buffer of metric samples</param>
    /// <param name="selector">Function to project each MetricSample to type T</param>
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
    /// <param name="samples">The new buffer snapshot to use for enumeration</param>
    public void UpdateBuffer(ImmutableCircularBuffer<MetricSample> samples)
    {
        _samples = samples;
    }

    /// <summary>
    /// Gets the number of samples in the buffer.
    /// </summary>
    public int Count => _samples.Count;

    /// <summary>
    /// Gets the projected value at the specified index. O(1) operation.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The projected value at the specified index</returns>
    public T this[int index] => _selector(_samples[index]);

    /// <summary>
    /// Gets the last (most recent) projected value. O(1) operation.
    /// Throws InvalidOperationException if the buffer is empty.
    /// </summary>
    public T Last()
    {
        return _selector(_samples.Last);
    }

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
