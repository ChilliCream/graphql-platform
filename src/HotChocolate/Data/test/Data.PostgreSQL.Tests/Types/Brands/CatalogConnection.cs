using GreenDonut.Data;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Brands;

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
                var items = _page.Items;
                var edges = new CatalogEdge<TEntity>[items.Length];

                for (var i = 0; i < items.Length; i++)
                {
                    edges[i] = new CatalogEdge<TEntity>(_page, items[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    /// <summary>
    /// A flattened list of the nodes.
    /// </summary>
    public IReadOnlyList<TEntity> Nodes => _page.Items;

    /// <summary>
    /// Information to aid in pagination.
    /// </summary>
    public override ConnectionPageInfo PageInfo
    {
        get
        {
            if (_pageInfo is null)
            {
                string? startCursor = null;
                string? endCursor = null;

                if (_page.First is not null)
                {
                    startCursor = _page.CreateCursor(_page.First);
                }

                if (_page.Last is not null)
                {
                    endCursor = _page.CreateCursor(_page.Last);
                }

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
