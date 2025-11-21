namespace Backend.Collectors;

/// <summary>
/// RAM metrics containing both percentage and absolute byte values.
/// </summary>
public readonly record struct RamMetrics
{
    /// <summary>
    /// Percentage of RAM currently in use (0-100).
    /// </summary>
    public float UsedPercent { get; init; }

    /// <summary>
    /// Amount of RAM currently in use, in bytes.
    /// </summary>
    public ulong UsedBytes { get; init; }

    /// <summary>
    /// Total system RAM, in bytes.
    /// </summary>
    public ulong TotalBytes { get; init; }
}
