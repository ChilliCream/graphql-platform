using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Products;

/// <summary>
/// An edge in a connection.
/// </summary>
public class ProductsEdge(Page<Product> page, PageEntry<Product> entry) : IEdge<Product>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    public Product Node => entry.Item;

    object? IEdge.Node => Node;

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    public string Cursor => page.CreateCursor(entry);
}
