using System.Collections;
using System.Collections.Immutable;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Immutable circular buffer backed by ImmutableQueue.
/// Thread-safe for reads without locks (immutable collections are inherently thread-safe).
/// </summary>
public sealed class ImmutableCircularBuffer<T> : IEnumerable<T>
{
    private readonly ImmutableQueue<T> _queue;
    private readonly int _capacity;

    public ImmutableCircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));

        _capacity = capacity;
        _queue = ImmutableQueue<T>.Empty;
    }

    private ImmutableCircularBuffer(ImmutableQueue<T> queue, int capacity)
    {
        _queue = queue;
        _capacity = capacity;
    }

    /// <summary>
    /// Adds an item to the buffer. Returns a new ImmutableCircularBuffer instance.
    /// If the buffer is at capacity, the oldest item is removed.
    /// </summary>
    public ImmutableCircularBuffer<T> Add(T item)
    {
        var newQueue = _queue.Enqueue(item);

        // If we exceed capacity, dequeue the oldest item
        if (newQueue.Count() > _capacity)
        {
            newQueue = newQueue.Dequeue();
        }

        return new ImmutableCircularBuffer<T>(newQueue, _capacity);
    }

    /// <summary>
    /// Gets the number of items currently in the buffer.
    /// </summary>
    public int Count => _queue.Count();

    /// <summary>
    /// Gets the maximum capacity of the buffer.
    /// </summary>
    public int Capacity => _capacity;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _queue)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
