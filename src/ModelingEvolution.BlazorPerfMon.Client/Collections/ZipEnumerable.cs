using System.Collections;

namespace ModelingEvolution.BlazorPerfMon.Client.Collections;

/// <summary>
/// Zero-allocation struct-based Zip implementation for two SampleAccessor instances.
/// Avoids LINQ Zip allocation overhead in hot rendering paths.
/// </summary>
/// <typeparam name="TFirst">Type of first accessor's projected values</typeparam>
/// <typeparam name="TSecond">Type of second accessor's projected values</typeparam>
internal readonly ref struct ZipEnumerable<TFirst, TSecond>
{
    private readonly SampleAccessor<TFirst> _first;
    private readonly SampleAccessor<TSecond> _second;

    public ZipEnumerable(SampleAccessor<TFirst> first, SampleAccessor<TSecond> second)
    {
        _first = first;
        _second = second;
    }

    public ZipEnumerator GetEnumerator() => new ZipEnumerator(_first, _second);

    public ref struct ZipEnumerator
    {
        private IEnumerator<TFirst> _firstEnumerator;
        private IEnumerator<TSecond> _secondEnumerator;
        private bool _hasValue;

        public ZipEnumerator(SampleAccessor<TFirst> first, SampleAccessor<TSecond> second)
        {
            _firstEnumerator = first.GetEnumerator();
            _secondEnumerator = second.GetEnumerator();
            _hasValue = false;
        }

        public bool MoveNext()
        {
            _hasValue = _firstEnumerator.MoveNext() && _secondEnumerator.MoveNext();
            return _hasValue;
        }

        public (TFirst First, TSecond Second) Current =>
            _hasValue
                ? (_firstEnumerator.Current, _secondEnumerator.Current)
                : throw new InvalidOperationException("Enumerator is not positioned on a valid element");

        public void Dispose()
        {
            _firstEnumerator?.Dispose();
            _secondEnumerator?.Dispose();
        }
    }
}

/// <summary>
/// General-purpose zero-allocation Zip implementation for any two IEnumerable instances.
/// Uses ref struct to avoid heap allocation.
/// </summary>
/// <typeparam name="TFirst">Type of first enumerable's values</typeparam>
/// <typeparam name="TSecond">Type of second enumerable's values</typeparam>
internal readonly ref struct GeneralZipEnumerable<TFirst, TSecond>
{
    private readonly IEnumerable<TFirst> _first;
    private readonly IEnumerable<TSecond> _second;

    public GeneralZipEnumerable(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
    {
        _first = first;
        _second = second;
    }

    public GeneralZipEnumerator GetEnumerator() => new GeneralZipEnumerator(_first, _second);

    public ref struct GeneralZipEnumerator
    {
        private IEnumerator<TFirst> _firstEnumerator;
        private IEnumerator<TSecond> _secondEnumerator;
        private bool _hasValue;

        public GeneralZipEnumerator(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            _firstEnumerator = first.GetEnumerator();
            _secondEnumerator = second.GetEnumerator();
            _hasValue = false;
        }

        public bool MoveNext()
        {
            _hasValue = _firstEnumerator.MoveNext() && _secondEnumerator.MoveNext();
            return _hasValue;
        }

        public (TFirst First, TSecond Second) Current =>
            _hasValue
                ? (_firstEnumerator.Current, _secondEnumerator.Current)
                : throw new InvalidOperationException("Enumerator is not positioned on a valid element");

        public void Dispose()
        {
            _firstEnumerator?.Dispose();
            _secondEnumerator?.Dispose();
        }
    }
}

/// <summary>
/// Extension methods for creating zero-allocation Zip enumerables.
/// </summary>
internal static class SampleAccessorExtensions
{
    /// <summary>
    /// Creates a zero-allocation Zip enumerable over two SampleAccessor instances.
    /// </summary>
    public static ZipEnumerable<TFirst, TSecond> ZipValues<TFirst, TSecond>(
        this SampleAccessor<TFirst> first,
        SampleAccessor<TSecond> second)
    {
        return new ZipEnumerable<TFirst, TSecond>(first, second);
    }

    /// <summary>
    /// Creates a zero-allocation Zip enumerable over a SampleAccessor and any IEnumerable.
    /// </summary>
    public static GeneralZipEnumerable<TFirst, TSecond> ZipValues<TFirst, TSecond>(
        this SampleAccessor<TFirst> first,
        IEnumerable<TSecond> second)
    {
        return new GeneralZipEnumerable<TFirst, TSecond>(first, second);
    }

    /// <summary>
    /// Creates a zero-allocation Zip enumerable over any two IEnumerable instances.
    /// </summary>
    public static GeneralZipEnumerable<TFirst, TSecond> ZipValues<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second)
    {
        return new GeneralZipEnumerable<TFirst, TSecond>(first, second);
    }
}
