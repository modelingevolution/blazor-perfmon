namespace ModelingEvolution.BlazorPerfMon.Client.Models;

/// <summary>
/// Efficient circular buffer for rolling window data storage.
/// Avoids array allocations by reusing a fixed-size buffer.
/// Thread-safe for single reader/writer scenarios.
/// </summary>
/// <typeparam name="T">Type of data stored in buffer</typeparam>
public sealed class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _writeIndex;
    private int _count;

    /// <summary>
    /// Creates a circular buffer with specified capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of items to store</param>
    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));

        _buffer = new T[capacity];
        _writeIndex = 0;
        _count = 0;
    }

    /// <summary>
    /// Gets the maximum capacity of the buffer.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Gets the current number of items in the buffer (0 to Capacity).
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Adds an item to the buffer. If the buffer is full, overwrites the oldest item.
    /// </summary>
    /// <param name="item">Item to add</param>
    public void Add(T item)
    {
        _buffer[_writeIndex] = item;
        _writeIndex = (_writeIndex + 1) % _buffer.Length;

        if (_count < _buffer.Length)
            _count++;
    }

    /// <summary>
    /// Gets an item at the specified index (0 = oldest, Count-1 = newest).
    /// </summary>
    /// <param name="index">Index of item to retrieve</param>
    /// <returns>Item at the specified index</returns>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Calculate actual buffer index accounting for wrap-around
            int actualIndex = (_writeIndex - _count + index + _buffer.Length) % _buffer.Length;
            return _buffer[actualIndex];
        }
    }

    /// <summary>
    /// Clears all items from the buffer.
    /// </summary>
    public void Clear()
    {
        _writeIndex = 0;
        _count = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }

    /// <summary>
    /// Gets the most recent item, or default if buffer is empty.
    /// </summary>
    public T? GetLatest()
    {
        if (_count == 0)
            return default;

        int latestIndex = (_writeIndex - 1 + _buffer.Length) % _buffer.Length;
        return _buffer[latestIndex];
    }

    /// <summary>
    /// Copies buffer contents to an array (oldest to newest).
    /// Useful for rendering operations.
    /// </summary>
    public T[] ToArray()
    {
        var result = new T[_count];
        for (int i = 0; i < _count; i++)
        {
            result[i] = this[i];
        }
        return result;
    }
}
