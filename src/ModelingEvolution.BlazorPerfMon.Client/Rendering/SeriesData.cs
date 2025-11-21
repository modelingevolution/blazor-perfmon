namespace Frontend.Rendering;

/// <summary>
/// Wraps enumerable data with count for chart rendering.
/// Avoids passing count separately and provides cleaner interface.
/// </summary>
public readonly struct SeriesData<T>
{
    public IEnumerable<T> Data { get; init; }
    public int Count { get; init; }

    public SeriesData(IEnumerable<T> data, int count)
    {
        Data = data;
        Count = count;
    }
}
