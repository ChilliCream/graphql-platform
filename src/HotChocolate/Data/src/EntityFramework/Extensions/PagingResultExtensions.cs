using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;

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
        return CreateConnection(result);
    }

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
    public static async ValueTask<Connection<T>> ToConnectionAsync<T>(
        this ValueTask<Page<T>> resultPromise)
        where T : class
    {
        var result = await resultPromise;
        return CreateConnection(result);
    }

    private static Connection<T> CreateConnection<T>(Page<T> page) where T : class
    {
        return new Connection<T>(
            page.Items.Select(t => new Edge<T>(t, page.CreateCursor)).ToArray(),
            new ConnectionPageInfo(
                page.HasPreviousPage,
                page.HasNextPage,
                CreateCursor(page.First, page.CreateCursor),
                CreateCursor(page.Last, page.CreateCursor)));
    }

    private static string? CreateCursor<T>(T? item, Func<T, string> createCursor) where T : class
        => item is null ? null : createCursor(item);
}
