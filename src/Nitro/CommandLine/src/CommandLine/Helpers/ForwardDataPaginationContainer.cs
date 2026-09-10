using static ChilliCream.Nitro.CommandLine.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Helpers;

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
