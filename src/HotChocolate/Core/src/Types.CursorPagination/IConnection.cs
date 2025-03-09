namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public interface IConnection : IPage
{
    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    IReadOnlyList<IEdge>? Edges { get; }
}

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public interface IConnection<out TNode> : IConnection
{
    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    new IReadOnlyList<IEdge<TNode>>? Edges { get; }
}
