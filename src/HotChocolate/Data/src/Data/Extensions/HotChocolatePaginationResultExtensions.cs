using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Extensions for <see cref="Page{T}"/>.
/// </summary>
public static class HotChocolatePaginationResultExtensions
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
    /// A factory that receives the source item and its cursor, and returns the edge.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
    public static async Task<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this Task<Page<TSource>> resultPromise,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }

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
    /// A factory that receives the source item, the source page, and the zero-based item index
    /// within that page, and returns the edge.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
    public static async Task<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this Task<Page<TSource>> resultPromise,
        Func<TSource, Page<TSource>, int, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
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
    /// <returns>
    /// Returns a relay connection.
    /// </returns>
    public static async ValueTask<Connection<T>> ToConnectionAsync<T>(
        this ValueTask<Page<T>> resultPromise)
        where T : class
    {
        var result = await resultPromise;
        return CreateConnection(result);
    }

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
    /// A factory that receives the source item and its cursor, and returns the edge.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
    public static async ValueTask<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this ValueTask<Page<TSource>> resultPromise,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        var result = await resultPromise;
        return CreateConnection(result, createEdge);
    }

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
    /// A factory that receives the source item, the source page, and the zero-based item index
    /// within that page, and returns the edge.
    /// </param>
    /// <returns>
    ///  Returns a relay connection.
    /// </returns>
    public static async ValueTask<Connection<TTarget>> ToConnectionAsync<TSource, TTarget>(
        this ValueTask<Page<TSource>> resultPromise,
        Func<TSource, Page<TSource>, int, Edge<TTarget>> createEdge)
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
        var edges = page.Items
            .Select((item, index) => new Edge<T>(item, index, page.CreateCursor))
            .ToArray();

        return new Connection<T>(
            edges,
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                page.CreateStartCursor(),
                page.CreateEndCursor()),
            page.TotalCount ?? 0);
    }

    private static Connection<TTarget> CreateConnection<TSource, TTarget>(
        Page<TSource>? page,
        Func<TSource, string, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        page ??= Page<TSource>.Empty;
        var items = page.Items;
        var edges = new Edge<TTarget>[items.Length];

        for (var i = 0; i < items.Length; i++)
        {
            edges[i] = createEdge(items[i], page.CreateCursor(i));
        }

        return new Connection<TTarget>(
            edges,
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                page.CreateStartCursor(),
                page.CreateEndCursor()),
            page.TotalCount ?? 0);
    }

    private static Connection<TTarget> CreateConnection<TSource, TTarget>(
        Page<TSource>? page,
        Func<TSource, Page<TSource>, int, Edge<TTarget>> createEdge)
        where TTarget : class
        where TSource : class
    {
        page ??= Page<TSource>.Empty;
        var items = page.Items;
        var edges = new Edge<TTarget>[items.Length];

        for (var i = 0; i < items.Length; i++)
        {
            edges[i] = createEdge(items[i], page, i);
        }

        return new Connection<TTarget>(
            edges,
            new ConnectionPageInfo(
                page.HasNextPage,
                page.HasPreviousPage,
                page.CreateStartCursor(),
                page.CreateEndCursor()),
            page.TotalCount ?? 0);
    }
}
