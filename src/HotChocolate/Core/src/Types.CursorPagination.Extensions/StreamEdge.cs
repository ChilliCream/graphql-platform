namespace HotChocolate.Types.Pagination;

/// <summary>
/// An edge in a streamed connection.
/// </summary>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Edge")]
public sealed class StreamEdge<TNode>(TNode node, string cursor) : IEdge<TNode>
{
    /// <summary>
    /// The item at the end of the edge.
    /// </summary>
    [GraphQLDescription("The item at the end of the edge.")]
    public TNode Node { get; } = node;

    /// <summary>
    /// A cursor for use in pagination.
    /// </summary>
    [GraphQLDescription("A cursor for use in pagination.")]
    public string Cursor { get; } = cursor;

    object? IEdge.Node => Node;
}
