using GreenDonut.Data;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Brands;

/// <summary>
/// An edge in a connection.
/// </summary>
[GraphQLName("{0}Edge")]
public class CatalogEdge<TEntity>(Page<TEntity> page, TEntity node) : IEdge<TEntity>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    public TEntity Node { get; } = node;

    object? IEdge.Node => Node;

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    public string Cursor => page.CreateCursor(Node);
}
