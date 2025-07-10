namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an edge in a connection.
/// </summary>
public interface IEdge<out T> : IEdge
{
    /// <summary>
    /// Gets the node.
    /// </summary>
    new T Node { get; }
}
