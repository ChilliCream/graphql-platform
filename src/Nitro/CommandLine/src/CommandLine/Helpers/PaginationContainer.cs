using ChilliCream.Nitro.Client;
using StrawberryShake;
using static ChilliCream.Nitro.CommandLine.ThrowHelper;

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

internal delegate Task<IOperationResult<TResult>> FetchAsync<TResult>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default) where TResult : class;

internal delegate Task<TResult> FetchDataAsync<TResult>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default) where TResult : class;

internal delegate Task<ConnectionPage<TItem>> FetchConnectionDataAsync<TItem>(
    string? after,
    int? first,
    CancellationToken cancellationToken = default);

internal delegate IPaginationPageInfo? SelectPageInfo<in TResult>(TResult result);

internal delegate IEnumerable<TEdge>? SelectEdges<out TEdge, in TResult>(TResult result);

internal interface IPaginationPageInfo
{
    bool HasNextPage { get; }

    string? EndCursor { get; }
}

internal class ForwardPaginationContainer<TResult, TEdge>
    : IPaginationContainer<TEdge>
    where TResult : class
{
    private int _pageSize = 5;
    private IPaginationPageInfo? _latestPageInfo;
    private int _currentPage = -1;

    private readonly List<IReadOnlyList<TEdge>> _pages = [];

    private readonly FetchAsync<TResult> _fetchAsync;
    private readonly SelectPageInfo<TResult> _pageInfoSelector;
    private readonly SelectEdges<TEdge, TResult> _selectEdgesSelector;

    public ForwardPaginationContainer(
        FetchAsync<TResult> fetchAsync,
        SelectPageInfo<TResult> pageInfoSelector,
        SelectEdges<TEdge, TResult> selectEdgesSelector)
    {
        _fetchAsync = fetchAsync;
        _pageInfoSelector = pageInfoSelector;
        _selectEdgesSelector = selectEdgesSelector;
    }

    public IPaginationContainer<TEdge> PageSize(int size)
    {
        if (_currentPage != -1)
        {
            throw new ExitException("Cannot change page size after initialization");
        }

        _pageSize = size;
        return this;
    }

    public bool HasNext()
        => _currentPage < _pages.Count - 1
        || (_latestPageInfo?.HasNextPage is not false && _currentPage == _pages.Count - 1);

    public bool HasPrevious() => _currentPage > 0;

    public async ValueTask<IReadOnlyList<TEdge>> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        if (_currentPage == -1)
        {
            return await FetchNextAsync(cancellationToken);
        }

        return _pages[_currentPage];
    }

    public async ValueTask<IReadOnlyList<TEdge>> FetchNextAsync(CancellationToken cancellationToken)
    {
        if (!HasNext())
        {
            throw new ExitException(
                "The end of the connection was reached, but Nitro tried to fetch next");
        }

        if (_currentPage < _pages.Count - 1)
        {
            return _pages[++_currentPage];
        }

        _currentPage++;

        var result = await _fetchAsync(_latestPageInfo?.EndCursor, _pageSize, cancellationToken);

        if (result.Data is not { } data)
        {
            if (result.Errors.FirstOrDefault() is { } error)
            {
                throw new ExitException(error.Message);
            }

            throw new ExitException("There was an issue with the request to the server.");
        }

        _latestPageInfo = _pageInfoSelector(data) ?? throw NoPageInfoFound();
        var edges = _selectEdgesSelector(data)?.ToArray() ?? throw CouldNotSelectEdges();
        _pages.Add(edges);

        return edges;
    }

    public ValueTask<IReadOnlyList<TEdge>> FetchPreviousAsync(CancellationToken cancellationToken)
    {
        if (!HasPrevious())
        {
            throw new ExitException(
                "The start of the connection was reached, but Nitro tried to fetch previous");
        }

        return new ValueTask<IReadOnlyList<TEdge>>(_pages[--_currentPage]);
    }
}

internal sealed class ConnectionPageInfo(string? endCursor, bool hasNextPage) : IPaginationPageInfo
{
    public bool HasNextPage { get; } = hasNextPage;

    public string? EndCursor { get; } = endCursor;
}

internal class ForwardDataPaginationContainer<TResult, TEdge>
    : IPaginationContainer<TEdge>
    where TResult : class
{
    private int _pageSize = 5;
    private IPaginationPageInfo? _latestPageInfo;
    private int _currentPage = -1;

    private readonly List<IReadOnlyList<TEdge>> _pages = [];

    private readonly FetchDataAsync<TResult> _fetchAsync;
    private readonly SelectPageInfo<TResult> _pageInfoSelector;
    private readonly SelectEdges<TEdge, TResult> _selectEdgesSelector;

    public ForwardDataPaginationContainer(
        FetchDataAsync<TResult> fetchAsync,
        SelectPageInfo<TResult> pageInfoSelector,
        SelectEdges<TEdge, TResult> selectEdgesSelector)
    {
        _fetchAsync = fetchAsync;
        _pageInfoSelector = pageInfoSelector;
        _selectEdgesSelector = selectEdgesSelector;
    }

    public IPaginationContainer<TEdge> PageSize(int size)
    {
        if (_currentPage != -1)
        {
            throw new ExitException("Cannot change page size after initialization");
        }

        _pageSize = size;
        return this;
    }

    public bool HasNext()
        => _currentPage < _pages.Count - 1
        || (_latestPageInfo?.HasNextPage is not false && _currentPage == _pages.Count - 1);

    public bool HasPrevious() => _currentPage > 0;

    public async ValueTask<IReadOnlyList<TEdge>> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        if (_currentPage == -1)
        {
            return await FetchNextAsync(cancellationToken);
        }

        return _pages[_currentPage];
    }

    public async ValueTask<IReadOnlyList<TEdge>> FetchNextAsync(CancellationToken cancellationToken)
    {
        if (!HasNext())
        {
            throw new ExitException(
                "The end of the connection was reached, but Nitro tried to fetch next");
        }

        if (_currentPage < _pages.Count - 1)
        {
            return _pages[++_currentPage];
        }

        _currentPage++;

        var data = await _fetchAsync(_latestPageInfo?.EndCursor, _pageSize, cancellationToken);

        _latestPageInfo = _pageInfoSelector(data) ?? throw NoPageInfoFound();
        var edges = _selectEdgesSelector(data)?.ToArray() ?? throw CouldNotSelectEdges();
        _pages.Add(edges);

        return edges;
    }

    public ValueTask<IReadOnlyList<TEdge>> FetchPreviousAsync(CancellationToken cancellationToken)
    {
        if (!HasPrevious())
        {
            throw new ExitException(
                "The start of the connection was reached, but Nitro tried to fetch previous");
        }

        return new ValueTask<IReadOnlyList<TEdge>>(_pages[--_currentPage]);
    }
}
