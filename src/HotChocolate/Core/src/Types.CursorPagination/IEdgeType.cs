namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents an edge in a connection.
/// </summary>
public interface IEdgeType : IObjectType
{
    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    string ConnectionName { get; }

    /// <summary>
    /// Gets the item type of the node field on the edge type.
    /// </summary>
    IOutputType NodeType { get; }
}
