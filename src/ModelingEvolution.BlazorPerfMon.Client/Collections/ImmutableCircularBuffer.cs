using System.Collections;
using System.Collections.Immutable;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Immutable circular buffer backed by ImmutableArray.
/// Thread-safe for reads without locks (immutable collections are inherently thread-safe).
///
/// DESIGN NOTE: Uses ImmutableArray for lock-free thread-safety and O(1) index access.
/// This is an intentional trade-off: each Add() creates a new ImmutableCircularBuffer instance,
/// which eliminates lock contention between the metrics collection thread and the UI render loop.
/// The alternative (using a mutable collection with locks) would introduce lock contention
/// in the hot rendering path (60 FPS), which is far more costly than GC pressure from allocations.
/// ImmutableArray provides fast index-based access which enables zero-allocation Zip operations.
/// </summary>
/// <typeparam name="T">The type of elements stored in the buffer</typeparam>
public sealed class ImmutableCircularBuffer<T> : IEnumerable<T>
{
    private readonly ImmutableArray<T> _array;
    private readonly int _capacity;
    private readonly int _count;
    private readonly T? _last;

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
        _array = ImmutableArray<T>.Empty;
        _count = 0;
        _last = default;
    }

    private ImmutableCircularBuffer(ImmutableArray<T> array, int capacity, int count, T? last)
    {
        _array = array;
        _capacity = capacity;
        _count = count;
        _last = last;
    }

    /// <summary>
    /// Adds an item to the buffer. Returns a new ImmutableCircularBuffer instance.
    /// If the buffer is at capacity, the oldest item is removed.
    /// </summary>
    /// <param name="item">The item to add to the buffer</param>
    /// <returns>A new ImmutableCircularBuffer instance containing the added item</returns>
    public ImmutableCircularBuffer<T> Add(T item)
    {
        var newArray = _array.Add(item);
        int newCount = _count + 1;

        // If we exceed capacity, remove the oldest item (first element)
        if (newCount > _capacity)
        {
            newArray = newArray.RemoveAt(0);
            newCount = _capacity;
        }

        return new ImmutableCircularBuffer<T>(newArray, _capacity, newCount, item);
    }

    /// <summary>
    /// Gets the number of items currently in the buffer. O(1) operation.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the maximum capacity of the buffer.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the last (most recently added) item in the buffer. O(1) operation.
    /// Throws InvalidOperationException if the buffer is empty.
    /// </summary>
    public T Last
    {
        get
        {
            if (_count == 0)
                throw new InvalidOperationException("Buffer is empty");
            return _last!;
        }
    }

    /// <summary>
    /// Gets the element at the specified index. O(1) operation.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get</param>
    /// <returns>The element at the specified index</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when index is out of range</exception>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for buffer with {_count} elements");
            return _array[index];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _array)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
