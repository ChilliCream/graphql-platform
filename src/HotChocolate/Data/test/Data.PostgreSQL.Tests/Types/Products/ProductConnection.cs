using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Products;

/// <summary>
/// A connection to a list of items.
/// </summary>
public class ProductConnection : ConnectionBase<Product, ProductsEdge, ConnectionPageInfo>
{
    private readonly Page<Product> _page;
    private ConnectionPageInfo? _pageInfo;
    private ProductsEdge[]? _edges;

    public ProductConnection(Page<Product> page)
    {
        _page = page;
    }

    /// <summary>
    /// A list of edges.
    /// </summary>
    public override IReadOnlyList<ProductsEdge>? Edges
    {
        get
        {
            if (_edges is null)
            {
                var entries = _page.Entries;
                var edges = new ProductsEdge[entries.Length];

                for (var i = 0; i < entries.Length; i++)
                {
                    edges[i] = new ProductsEdge(_page, entries[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    /// <summary>
    /// A flattened list of the nodes.
    /// </summary>
    public IReadOnlyList<Product>? Nodes => _page;

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

    [GraphQLType<NonNullType<ListType<NonNullType<StringType>>>>]
    public IEnumerable<string> GetEndCursors(int count)
    {
        if (_page.Count == 0)
        {
            yield break;
        }

        var lastEntry = _page.Entries[^1];

        for (var i = 0; i < count; i++)
        {
            yield return _page.CreateCursor(lastEntry, i);
        }
    }
}
