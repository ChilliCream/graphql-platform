using HotChocolate.Types.Pagination;

namespace HotChocolate.Data;

/// <summary>
/// Extensions for <see cref="Page{T}"/>.
/// </summary>
public static class PagingResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Page{T}"/> to a <see cref="Connection{T}"/>.
    /// </summary>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the page.
    /// </typeparam>
    /// <returns></returns>
    public static async Task<Connection<T>> ToConnectionAsync<T>(
        this Task<Page<T>> resultPromise)
        where T : class
    {
        var result = await resultPromise;
        
        return new Connection<T>(
            result.Items.Select(t => new Edge<T>(t, result.CreateCursor)).ToArray(),
            new ConnectionPageInfo(
                result.HasPreviousPage,
                result.HasNextPage,
                CreateCursor(result.First, result.CreateCursor),
                CreateCursor(result.Last, result.CreateCursor)));
    }

    private static string? CreateCursor<T>(T? item, Func<T, string> createCursor) where T : class
        => item is null ? null : createCursor(item);
}