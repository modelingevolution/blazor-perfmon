using System.Collections;
using System.Collections.Immutable;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Immutable circular buffer backed by ImmutableQueue.
/// Thread-safe for reads without locks (immutable collections are inherently thread-safe).
///
/// DESIGN NOTE: Uses ImmutableQueue for lock-free thread-safety at the cost of allocation.
/// This is an intentional trade-off: each Add() creates a new ImmutableCircularBuffer instance,
/// which eliminates lock contention between the metrics collection thread and the UI render loop.
/// The alternative (using a mutable collection with locks) would introduce lock contention
/// in the hot rendering path (60 FPS), which is far more costly than GC pressure from allocations.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer</typeparam>
public sealed class ImmutableCircularBuffer<T> : IEnumerable<T>
{
    private readonly ImmutableQueue<T> _queue;
    private readonly int _capacity;
    private readonly int _count;

    /// <summary>
    /// Initializes a new instance of the ImmutableCircularBuffer class.
    /// </summary>
    /// <param name="capacity">The maximum number of elements the buffer can hold</param>
    /// <exception cref="ArgumentException">Thrown when capacity is less than or equal to zero</exception>
    public ImmutableCircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero", nameof(capacity));

        _capacity = capacity;
        _queue = ImmutableQueue<T>.Empty;
        _count = 0;
    }

    private ImmutableCircularBuffer(ImmutableQueue<T> queue, int capacity, int count)
    {
        _queue = queue;
        _capacity = capacity;
        _count = count;
    }

    /// <summary>
    /// Adds an item to the buffer. Returns a new ImmutableCircularBuffer instance.
    /// If the buffer is at capacity, the oldest item is removed.
    /// </summary>
    /// <param name="item">The item to add to the buffer</param>
    /// <returns>A new ImmutableCircularBuffer instance containing the added item</returns>
    public ImmutableCircularBuffer<T> Add(T item)
    {
        var newQueue = _queue.Enqueue(item);
        int newCount = _count + 1;

        // If we exceed capacity, dequeue the oldest item
        if (newCount > _capacity)
        {
            newQueue = newQueue.Dequeue();
            newCount = _capacity;
        }

        return new ImmutableCircularBuffer<T>(newQueue, _capacity, newCount);
    }

    /// <summary>
    /// Gets the number of items currently in the buffer. O(1) operation.
    /// </summary>
    public int Count => _count;

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
