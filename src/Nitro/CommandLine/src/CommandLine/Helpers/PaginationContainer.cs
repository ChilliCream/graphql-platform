using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class PaginationContainer
{
    public static IPaginationContainer<TEdge> Create<TResult, TEdge>(
        FetchAsync<TResult> fetchAsync,
        SelectPageInfo<TResult> pageInfoSelector,
        SelectEdges<TEdge, TResult> selectEdgesSelector) where TResult : class
    {
        return new ForwardPaginationContainer<TResult, TEdge>(
            fetchAsync,
            pageInfoSelector,
            selectEdgesSelector);
    }

    public static IPaginationContainer<TEdge> CreateData<TResult, TEdge>(
        FetchDataAsync<TResult> fetchAsync,
        SelectPageInfo<TResult> pageInfoSelector,
        SelectEdges<TEdge, TResult> selectEdgesSelector) where TResult : class
    {
        return new ForwardDataPaginationContainer<TResult, TEdge>(
            fetchAsync,
            pageInfoSelector,
            selectEdgesSelector);
    }

    public static IPaginationContainer<TItem> CreateConnectionData<TItem>(
        FetchConnectionDataAsync<TItem> fetchAsync)
    {
        return new ForwardDataPaginationContainer<ConnectionPage<TItem>, TItem>(
            (after, first, ct) => fetchAsync(after, first, ct),
            static p => new ConnectionPageInfo(p.EndCursor, p.HasNextPage),
            static p => p.Items);
    }
}
