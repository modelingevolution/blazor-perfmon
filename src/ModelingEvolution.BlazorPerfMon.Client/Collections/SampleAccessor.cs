using System.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Delegate for projecting a metric sample to a value of type T.
/// Uses 'in' parameter for efficient pass-by-reference without copying.
/// </summary>
/// <param name="sample">The metric sample to project</param>
/// <returns>The projected value</returns>
public delegate T MetricAccessorF<T>(in MetricSample sample);

/// <summary>
/// Delegate for projecting metric samples to a value of type T using delta calculation.
/// Uses 'in' parameter for efficient pass-by-reference without copying.
/// Receives both current and previous samples to enable delta/rate calculations.
/// </summary>
/// <param name="current">The current metric sample</param>
/// <param name="previous">The previous metric sample (default if first sample)</param>
/// <returns>The projected value (typically a delta or rate)</returns>
public delegate T MetricDeltaAccessorF<T>(in MetricSample current, in MetricSample previous);

/// <summary>
/// Zero-copy adapter/facade over ImmutableCircularBuffer with projection selector.
/// Provides IEnumerable without allocating arrays - selector is applied during enumeration.
/// Can be updated with a new buffer reference for time-consistent rendering.
/// </summary>
/// <typeparam name="T">The type of projected values returned by the selector.</typeparam>
internal sealed class SampleAccessor<T> : IEnumerable<T>
{
    private ImmutableCircularBuffer<MetricSample> _samples;
    private readonly MetricAccessorF<T> _selector;

    /// <summary>
    /// Initializes a new instance of the SampleAccessor class.
    /// </summary>
    /// <param name="samples">The initial buffer of metric samples</param>
    /// <param name="selector">Function to project each MetricSample to type T</param>
    public SampleAccessor(
        ImmutableCircularBuffer<MetricSample> samples,
        MetricAccessorF<T> selector)
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

/// <summary>
/// Zero-copy adapter for delta/rate calculations requiring access to consecutive samples.
/// Provides IEnumerable without allocating arrays - selector receives both current and previous samples.
/// Can be updated with a new buffer reference for time-consistent rendering.
/// </summary>
/// <typeparam name="T">The type of projected values returned by the selector.</typeparam>
internal sealed class SampleDeltaAccessor<T> : IEnumerable<T>
{
    private ImmutableCircularBuffer<MetricSample> _samples;
    private readonly MetricDeltaAccessorF<T> _selector;

    /// <summary>
    /// Initializes a new instance of the SampleDeltaAccessor class.
    /// </summary>
    /// <param name="samples">The initial buffer of metric samples</param>
    /// <param name="selector">Function to project current and previous MetricSample to type T</param>
    public SampleDeltaAccessor(
        ImmutableCircularBuffer<MetricSample> samples,
        MetricDeltaAccessorF<T> selector)
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
    /// Compares sample at index with sample at index-1 (or default if index is 0).
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The projected value at the specified index</returns>
    public T this[int index]
    {
        get
        {
            var current = _samples[index];
            var previous = index > 0 ? _samples[index - 1] : default;
            return _selector(current, previous);
        }
    }

    /// <summary>
    /// Gets the last (most recent) projected value. O(1) operation.
    /// Compares last sample with second-to-last (or default if only one sample).
    /// Throws InvalidOperationException if the buffer is empty.
    /// </summary>
    public T Last()
    {
        int lastIndex = _samples.Count - 1;
        var current = _samples.Last;
        var previous = lastIndex > 0 ? _samples[lastIndex - 1] : default;
        return _selector(current, previous);
    }

    /// <summary>
    /// Enumerates the projected values without allocating arrays.
    /// The selector is applied on-the-fly with access to consecutive samples for delta calculations.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        MetricSample previous = default;
        bool isFirst = true;

        foreach (var current in _samples)
        {
            yield return _selector(current, isFirst ? default : previous);
            previous = current;
            isFirst = false;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
