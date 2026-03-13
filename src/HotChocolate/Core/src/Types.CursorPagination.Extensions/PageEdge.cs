using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// An edge in a connection.
/// </summary>
/// <param name="page">
/// The page that contains the node.
/// </param>
/// <param name="index">
/// The zero-based index of the node within the page.
/// </param>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Edge")]
public class PageEdge<TNode>(Page<TNode> page, int index) : IEdge<TNode>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    [GraphQLDescription("The item at the end of the edge.")]
    public TNode Node => page.Items[index];

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    [GraphQLDescription("A cursor for use in pagination.")]
    public string Cursor => page.CreateCursor(index);

    object? IEdge.Node => Node;
}
