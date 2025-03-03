using System.Diagnostics.CodeAnalysis;
using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Products;


[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsAsync(pagingArgs, query, cancellationToken);
        return new ProductConnection(page);
    }
}

public class ProductConnection : ConnectionBase<Product, ProductEdge, ConnectionPageInfo>
{
    private readonly Page<Product> _page;
    private ConnectionPageInfo? _pageInfo;
    private ProductEdge[]? _edges;

    public ProductConnection(Page<Product> page)
    {
        _page = page;
    }

    public override IReadOnlyList<ProductEdge> Edges
    {
        get
        {
            if (_edges is null)
            {
                var items = _page.Items;
                var edges = new ProductEdge[items.Length];

                for (var i = 0; i < items.Length; i++)
                {
                    edges[i] = new ProductEdge(_page, items[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    public IReadOnlyList<Product> Nodes => _page.Items;

    public override ConnectionPageInfo PageInfo
    {
        get
        {
            if (_pageInfo is null)
            {
                string? startCursor = null;
                string? endCursor = null;

                if(_page.First is not null)
                {
                    startCursor = _page.CreateCursor(_page.First);
                }

                if(_page.Last is not null)
                {
                    endCursor = _page.CreateCursor(_page.Last);
                }

                _pageInfo = new ConnectionPageInfo(_page.HasNextPage, _page.HasPreviousPage, startCursor, endCursor);
            }

            return _pageInfo;
        }
    }

    public int TotalCount => _page.TotalCount ?? 0;
}

public class ProductEdge(Page<Product> page, Product node) : IEdge<Product>
{
    public Product Node { get; } = node;

    object? IEdge.Node => Node;

    public string Cursor => page.CreateCursor(Node);
}
