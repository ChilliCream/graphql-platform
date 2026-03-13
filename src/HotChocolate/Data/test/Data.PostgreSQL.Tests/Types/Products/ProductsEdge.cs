using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Products;

/// <summary>
/// An edge in a connection.
/// </summary>
public class ProductsEdge(Page<Product> page, int index) : IEdge<Product>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    public Product Node => page.Items[index];

    object? IEdge.Node => Node;

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    public string Cursor => page.CreateCursor(index);
}
