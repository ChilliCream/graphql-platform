using HotChocolate.Types.Pagination;

namespace HotChocolate.Pagination;

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
    /// <returns>
    /// Returns a relay connection.
    /// </returns>
#nullable disable
    public static async Task<Connection<T>> ToConnectionAsync<T>(
        this Task<Page<T>> resultPromise)
        where T : class
    {
        var result = await resultPromise;
        return CreateConnection(result);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{TSource}"/> to a <see cref="Connection{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source entity type.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target entity type.
    /// </typeparam>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <param name="createEdge">
    /// A factory to create an edge from a source entity.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
#nullable disable
    public static async Task<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this Task<Page<TSource>> resultPromise,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{TSource}"/> to a <see cref="Connection{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source entity type.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target entity type.
    /// </typeparam>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <param name="createEdge">
    /// A factory to create an edge from a source entity.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
#nullable disable
    public static async Task<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this Task<Page<TSource>> resultPromise,
        Func<TSource, Page<TSource>, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{T}"/> to a <see cref="Connection{T}"/>.
    /// </summary>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the page.
    /// </typeparam>
    /// <returns>
    /// Returns a relay connection.
    /// </returns>
#nullable disable
    public static async ValueTask<Connection<T>> ToConnectionAsync<T>(
        this ValueTask<Page<T>> resultPromise)
        where T : class
    {
        var result = await resultPromise;
        return CreateConnection(result);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{TSource}"/> to a <see cref="Connection{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source entity type.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target entity type.
    /// </typeparam>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <param name="createEdge">
    /// A factory to create an edge from a source entity.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
#nullable disable
    public static async ValueTask<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this ValueTask<Page<TSource>> resultPromise,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{TSource}"/> to a <see cref="Connection{TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source entity type.
    /// </typeparam>
    /// <typeparam name="TTarget">
    /// The target entity type.
    /// </typeparam>
    /// <param name="resultPromise">
    /// The page result.
    /// </param>
    /// <param name="createEdge">
    /// A factory to create an edge from a source entity.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
#nullable disable
    public static async ValueTask<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this ValueTask<Page<TSource>> resultPromise,
        Func<TSource, Page<TSource>, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }
#nullable restore

    /// <summary>
    /// Converts a <see cref="Page{T}"/> to a <see cref="Connection{T}"/>.
    /// </summary>
    /// <param name="result">
    /// The page result.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the page.
    /// </typeparam>
    /// <returns>
    /// Returns a relay connection.
    /// </returns>
    public static Connection<T> ToConnection<T>(
        this Page<T> result)
        where T : class
        => CreateConnection(result);

    private static Connection<T> CreateConnection<T>(Page<T>? page) where T : class
    {
        page ??= Page<T>.Empty;

        return new Connection<T>(
            page.Items.Select(t => new Edge<T>(t, page.CreateCursor)).ToArray(),
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                CreateCursor(page.First, page.CreateCursor),
                CreateCursor(page.Last, page.CreateCursor)),
            page.TotalCount ?? 0);
    }

    private static Connection<TTarget> CreateConnection<TSource, TTarget>(
        Page<TSource>? page,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        page ??= Page<TSource>.Empty;

        return new Connection<TTarget>(
            page.Items.Select(t => createEdge(t, page.CreateCursor(t))).ToArray(),
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                CreateCursor(page.First, page.CreateCursor),
                CreateCursor(page.Last, page.CreateCursor)),
            page.TotalCount ?? 0);
    }

    private static Connection<TTarget> CreateConnection<TSource, TTarget>(
        Page<TSource>? page,
        Func<TSource, Page<TSource>, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        page ??= Page<TSource>.Empty;

        return new Connection<TTarget>(
            page.Items.Select(t => createEdge(t, page)).ToArray(),
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                CreateCursor(page.First, page.CreateCursor),
                CreateCursor(page.Last, page.CreateCursor)),
            page.TotalCount ?? 0);
    }

    private static string? CreateCursor<T>(T? item, Func<T, string> createCursor) where T : class
        => item is null ? null : createCursor(item);
}
