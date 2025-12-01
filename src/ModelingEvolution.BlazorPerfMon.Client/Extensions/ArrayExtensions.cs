namespace ModelingEvolution.BlazorPerfMon.Client.Extensions;

/// <summary>
/// Extension methods for arrays.
/// </summary>
internal static class ArrayExtensions
{
    /// <summary>
    /// Finds the index of the first element matching the predicate.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="array">The array to search</param>
    /// <param name="predicate">The matching predicate</param>
    /// <returns>Index of first match, or -1 if not found</returns>
    public static int IndexOf<T>(this T[] array, Func<T, bool> predicate)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (predicate(array[i]))
                return i;
        }
        return -1;
    }
}
