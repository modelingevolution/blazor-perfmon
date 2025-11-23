using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ModelingEvolution.BlazorPerfMon.Client.Collections;
using ModelingEvolution.BlazorPerfMon.Shared;
using System.Collections.Immutable;

namespace ModelingEvolution.BlazorPerfMon.Benchmarks;

/// <summary>
/// Benchmarks comparing ImmutableQueue, ImmutableArray, and ImmutableList for circular buffer implementation.
/// Tests both AddSample (when full) and Zip operations to mimic production usage.
/// Run with: dotnet run -c Release --project tests/ModelingEvolution.BlazorPerfMon.Benchmarks --filter *CircularBufferBenchmarks*
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class CircularBufferBenchmarks
{
    private CircularBufferQueue<MetricSample> _queueBuffer = null!;
    private CircularBufferArray<MetricSample> _arrayBuffer = null!;
    private CircularBufferList<MetricSample> _listBuffer = null!;
    private MetricSample _sampleToAdd;

    private SampleAccessor<float> _queueFloatAccessor = null!;
    private SampleAccessor<uint> _queueUintAccessor = null!;

    private SampleAccessorArray<float> _arrayFloatAccessor = null!;
    private SampleAccessorArray<uint> _arrayUintAccessor = null!;

    private SampleAccessorList<float> _listFloatAccessor = null!;
    private SampleAccessorList<uint> _listUintAccessor = null!;

    [Params(100, 200, 500)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        // Fill buffers to capacity
        _queueBuffer = new CircularBufferQueue<MetricSample>(N);
        _arrayBuffer = new CircularBufferArray<MetricSample>(N);
        _listBuffer = new CircularBufferList<MetricSample>(N);

        for (int i = 0; i < N; i++)
        {
            var sample = new MetricSample
            {
                CreatedAt = (uint)(1000 + i),
                CpuLoads = new[] { 50f + i },
                GpuLoads = new[] { 30f + i },
                Ram = new RamMetric { UsedBytes = 1000, TotalBytes = 2000 },
                CpuAverage = 50f + i,
                GpuAverage = 30f + i
            };
            _queueBuffer = _queueBuffer.Add(sample);
            _arrayBuffer = _arrayBuffer.Add(sample);
            _listBuffer = _listBuffer.Add(sample);
        }

        // Create sample to add (for AddSample benchmarks)
        _sampleToAdd = new MetricSample
        {
            CreatedAt = (uint)(1000 + N),
            CpuLoads = new[] { 50f + N },
            GpuLoads = new[] { 30f + N },
            Ram = new RamMetric { UsedBytes = 1000, TotalBytes = 2000 },
            CpuAverage = 50f + N,
            GpuAverage = 30f + N
        };

        // Create accessors for Zip benchmarks
        _queueFloatAccessor = new SampleAccessor<float>(_queueBuffer.AsImmutableCircularBuffer(), s => s.CpuAverage);
        _queueUintAccessor = new SampleAccessor<uint>(_queueBuffer.AsImmutableCircularBuffer(), s => s.CreatedAt);

        _arrayFloatAccessor = new SampleAccessorArray<float>(_arrayBuffer, s => s.CpuAverage);
        _arrayUintAccessor = new SampleAccessorArray<uint>(_arrayBuffer, s => s.CreatedAt);

        _listFloatAccessor = new SampleAccessorList<float>(_listBuffer, s => s.CpuAverage);
        _listUintAccessor = new SampleAccessorList<uint>(_listBuffer, s => s.CreatedAt);
    }

    // ============================================
    // AddSample Benchmarks (when capacity is full)
    // ============================================

    [Benchmark(Baseline = true)]
    public CircularBufferQueue<MetricSample> AddSample_ImmutableQueue()
    {
        return _queueBuffer.Add(_sampleToAdd);
    }

    [Benchmark]
    public CircularBufferArray<MetricSample> AddSample_ImmutableArray()
    {
        return _arrayBuffer.Add(_sampleToAdd);
    }

    [Benchmark]
    public CircularBufferList<MetricSample> AddSample_ImmutableList()
    {
        return _listBuffer.Add(_sampleToAdd);
    }

    // ============================================
    // Zip Benchmarks
    // ============================================

    [Benchmark]
    public int Zip_ImmutableQueue()
    {
        int count = 0;
        foreach (var (value, timestamp) in _queueFloatAccessor.ZipValues(_queueUintAccessor))
        {
            count++;
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }

    [Benchmark]
    public int Zip_ImmutableArray()
    {
        int count = 0;
        foreach (var (value, timestamp) in _arrayFloatAccessor.ZipValues(_arrayUintAccessor))
        {
            count++;
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }

    [Benchmark]
    public int Zip_ImmutableList()
    {
        int count = 0;
        foreach (var (value, timestamp) in _listFloatAccessor.ZipValues(_listUintAccessor))
        {
            count++;
            if (value > 0 && timestamp > 0) count += 0;
        }
        return count;
    }
}

// ============================================
// ImmutableQueue-based implementation (current)
// ============================================
public sealed class CircularBufferQueue<T>
{
    private readonly ImmutableQueue<T> _queue;
    private readonly int _capacity;
    private readonly int _count;
    private readonly T? _last;

    public CircularBufferQueue(int capacity)
    {
        _capacity = capacity;
        _queue = ImmutableQueue<T>.Empty;
        _count = 0;
        _last = default;
    }

    private CircularBufferQueue(ImmutableQueue<T> queue, int capacity, int count, T? last)
    {
        _queue = queue;
        _capacity = capacity;
        _count = count;
        _last = last;
    }

    public CircularBufferQueue<T> Add(T item)
    {
        var newQueue = _queue.Enqueue(item);
        int newCount = _count + 1;

        if (newCount > _capacity)
        {
            newQueue = newQueue.Dequeue();
            newCount = _capacity;
        }

        return new CircularBufferQueue<T>(newQueue, _capacity, newCount, item);
    }

    public int Count => _count;
    public ImmutableCircularBuffer<T> AsImmutableCircularBuffer()
    {
        var buffer = new ImmutableCircularBuffer<T>(_capacity);
        foreach (var item in _queue)
        {
            buffer = buffer.Add(item);
        }
        return buffer;
    }

    public IEnumerable<T> AsEnumerable() => _queue;
}

// ============================================
// ImmutableArray-based implementation
// ============================================
public sealed class CircularBufferArray<T>
{
    private readonly ImmutableArray<T> _array;
    private readonly int _capacity;
    private readonly int _count;
    private readonly T? _last;

    public CircularBufferArray(int capacity)
    {
        _capacity = capacity;
        _array = ImmutableArray<T>.Empty;
        _count = 0;
        _last = default;
    }

    private CircularBufferArray(ImmutableArray<T> array, int capacity, int count, T? last)
    {
        _array = array;
        _capacity = capacity;
        _count = count;
        _last = last;
    }

    public CircularBufferArray<T> Add(T item)
    {
        var newArray = _array.Add(item);
        int newCount = _count + 1;

        // If we exceed capacity, remove the first item (oldest)
        if (newCount > _capacity)
        {
            newArray = newArray.RemoveAt(0);
            newCount = _capacity;
        }

        return new CircularBufferArray<T>(newArray, _capacity, newCount, item);
    }

    public int Count => _count;
    public T this[int index] => _array[index];
    public ImmutableArray<T> AsArray() => _array;
}

// ============================================
// ImmutableList-based implementation
// ============================================
public sealed class CircularBufferList<T>
{
    private readonly ImmutableList<T> _list;
    private readonly int _capacity;
    private readonly int _count;
    private readonly T? _last;

    public CircularBufferList(int capacity)
    {
        _capacity = capacity;
        _list = ImmutableList<T>.Empty;
        _count = 0;
        _last = default;
    }

    private CircularBufferList(ImmutableList<T> list, int capacity, int count, T? last)
    {
        _list = list;
        _capacity = capacity;
        _count = count;
        _last = last;
    }

    public CircularBufferList<T> Add(T item)
    {
        var newList = _list.Add(item);
        int newCount = _count + 1;

        // If we exceed capacity, remove the first item (oldest)
        if (newCount > _capacity)
        {
            newList = newList.RemoveAt(0);
            newCount = _capacity;
        }

        return new CircularBufferList<T>(newList, _capacity, newCount, item);
    }

    public int Count => _count;
    public T this[int index] => _list[index];
    public ImmutableList<T> AsList() => _list;
}

// ============================================
// SampleAccessor variants with index access
// ============================================

/// <summary>
/// SampleAccessor for ImmutableArray with index-based access
/// </summary>
internal sealed class SampleAccessorArray<T>
{
    private readonly CircularBufferArray<MetricSample> _samples;
    private readonly Func<MetricSample, T> _selector;

    public SampleAccessorArray(CircularBufferArray<MetricSample> samples, Func<MetricSample, T> selector)
    {
        _samples = samples;
        _selector = selector;
    }

    public int Count => _samples.Count;
    public T this[int index] => _selector(_samples[index]);
}

/// <summary>
/// SampleAccessor for ImmutableList with index-based access
/// </summary>
internal sealed class SampleAccessorList<T>
{
    private readonly CircularBufferList<MetricSample> _samples;
    private readonly Func<MetricSample, T> _selector;

    public SampleAccessorList(CircularBufferList<MetricSample> samples, Func<MetricSample, T> selector)
    {
        _samples = samples;
        _selector = selector;
    }

    public int Count => _samples.Count;
    public T this[int index] => _selector(_samples[index]);
}

// ============================================
// Index-based Zip implementations
// ============================================

/// <summary>
/// Zero-allocation Zip using index access for ImmutableArray
/// </summary>
internal readonly ref struct ZipEnumerableArray<TFirst, TSecond>
{
    private readonly SampleAccessorArray<TFirst> _first;
    private readonly SampleAccessorArray<TSecond> _second;

    public ZipEnumerableArray(SampleAccessorArray<TFirst> first, SampleAccessorArray<TSecond> second)
    {
        _first = first;
        _second = second;
    }

    public ZipEnumeratorArray GetEnumerator() => new ZipEnumeratorArray(_first, _second);

    public ref struct ZipEnumeratorArray
    {
        private readonly SampleAccessorArray<TFirst> _first;
        private readonly SampleAccessorArray<TSecond> _second;
        private readonly int _count;
        private int _index;

        public ZipEnumeratorArray(SampleAccessorArray<TFirst> first, SampleAccessorArray<TSecond> second)
        {
            _first = first;
            _second = second;
            _count = Math.Min(first.Count, second.Count);
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public (TFirst First, TSecond Second) Current => (_first[_index], _second[_index]);

        public void Dispose() { }
    }
}

/// <summary>
/// Zero-allocation Zip using index access for ImmutableList
/// </summary>
internal readonly ref struct ZipEnumerableList<TFirst, TSecond>
{
    private readonly SampleAccessorList<TFirst> _first;
    private readonly SampleAccessorList<TSecond> _second;

    public ZipEnumerableList(SampleAccessorList<TFirst> first, SampleAccessorList<TSecond> second)
    {
        _first = first;
        _second = second;
    }

    public ZipEnumeratorList GetEnumerator() => new ZipEnumeratorList(_first, _second);

    public ref struct ZipEnumeratorList
    {
        private readonly SampleAccessorList<TFirst> _first;
        private readonly SampleAccessorList<TSecond> _second;
        private readonly int _count;
        private int _index;

        public ZipEnumeratorList(SampleAccessorList<TFirst> first, SampleAccessorList<TSecond> second)
        {
            _first = first;
            _second = second;
            _count = Math.Min(first.Count, second.Count);
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public (TFirst First, TSecond Second) Current => (_first[_index], _second[_index]);

        public void Dispose() { }
    }
}

/// <summary>
/// Extension methods for creating index-based Zip enumerables
/// </summary>
internal static class SampleAccessorArrayExtensions
{
    public static ZipEnumerableArray<TFirst, TSecond> ZipValues<TFirst, TSecond>(
        this SampleAccessorArray<TFirst> first,
        SampleAccessorArray<TSecond> second)
    {
        return new ZipEnumerableArray<TFirst, TSecond>(first, second);
    }

    public static ZipEnumerableList<TFirst, TSecond> ZipValues<TFirst, TSecond>(
        this SampleAccessorList<TFirst> first,
        SampleAccessorList<TSecond> second)
    {
        return new ZipEnumerableList<TFirst, TSecond>(first, second);
    }
}
