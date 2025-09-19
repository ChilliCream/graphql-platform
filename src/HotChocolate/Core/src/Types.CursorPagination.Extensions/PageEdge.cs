using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// An edge in a connection.
/// </summary>
/// <param name="page">
/// The page that contains the node.
/// </param>
/// <param name="node">
/// The node that is part of the edge.
/// </param>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Edge")]
public class PageEdge<TNode>(Page<TNode> page, TNode node) : IEdge<TNode>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    [GraphQLDescription("The item at the end of the edge.")]
    public TNode Node => node;

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    [GraphQLDescription("A cursor for use in pagination.")]
    public string Cursor => page.CreateCursor(node);

    object? IEdge.Node => Node;
}
