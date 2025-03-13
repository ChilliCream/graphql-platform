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
                var items = _page.Items;
                var edges = new ProductsEdge[items.Length];

                for (var i = 0; i < items.Length; i++)
                {
                    edges[i] = new ProductsEdge(_page, items[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    /// <summary>
    /// A flattened list of the nodes.
    /// </summary>
    public IReadOnlyList<Product>? Nodes => _page.Items;

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

    [GraphQLType<NonNullType<ListType<NonNullType<StringType>>>>]
    public IEnumerable<string> GetEndCursors(int count)
    {
        if (_page.Last is null)
        {
            yield break;
        }

        for (var i = 0; i < count; i++)
        {
            yield return _page.CreateCursor(_page.Last, i);
        }
    }
}
