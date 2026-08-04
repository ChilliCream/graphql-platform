using GreenDonut.Data;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Brands;

/// <summary>
/// Some connection docs
/// </summary>
[GraphQLName("{0}Connection")]
public class CatalogConnection<TEntity> : ConnectionBase<TEntity, CatalogEdge<TEntity>, ConnectionPageInfo>
{
    private readonly Page<TEntity> _page;
    private ConnectionPageInfo? _pageInfo;
    private CatalogEdge<TEntity>[]? _edges;

    public CatalogConnection(Page<TEntity> page)
    {
        _page = page;
    }

    /// <summary>
    /// A list of edges.
    /// </summary>
    public override IReadOnlyList<CatalogEdge<TEntity>> Edges
    {
        get
        {
            if (_edges is null)
            {
                var entries = _page.Entries;
                var edges = new CatalogEdge<TEntity>[entries.Length];

                for (var i = 0; i < entries.Length; i++)
                {
                    edges[i] = new CatalogEdge<TEntity>(_page, entries[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    /// <summary>
    /// A flattened list of the nodes.
    /// </summary>
    public IReadOnlyList<TEntity> Nodes => _page;

    /// <summary>
    /// Information to aid in pagination.
    /// </summary>
    public override ConnectionPageInfo PageInfo
    {
        get
        {
            if (_pageInfo is null)
            {
                var startCursor = _page.CreateStartCursor();
                var endCursor = _page.CreateEndCursor();

                _pageInfo = new ConnectionPageInfo(_page.HasNextPage, _page.HasPreviousPage, startCursor, endCursor);
            }

            return _pageInfo;
        }
    }

    /// <summary>
    /// Identifies the total count of items in the connection.
    /// </summary>
    public int TotalCount => _page.TotalCount ?? 0;
}
