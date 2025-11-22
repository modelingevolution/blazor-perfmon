namespace ModelingEvolution.BlazorPerfMon.Client.Rendering;

/// <summary>
/// Wraps enumerable data with count for chart rendering.
/// Avoids passing count separately and provides cleaner interface.
/// </summary>
/// <typeparam name="T">The type of data elements in the series</typeparam>
internal readonly struct SeriesData<T>
{
    /// <summary>
    /// Gets the enumerable data sequence.
    /// </summary>
    public IEnumerable<T> Data { get; init; }

    /// <summary>
    /// Gets the count of elements in the data sequence.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Initializes a new instance of the SeriesData struct.
    /// </summary>
    /// <param name="data">The data sequence</param>
    /// <param name="count">The count of elements in the data sequence</param>
    public SeriesData(IEnumerable<T> data, int count)
    {
        Data = data;
        Count = count;
    }
}
