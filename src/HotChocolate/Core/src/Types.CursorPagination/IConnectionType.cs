namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection type.
/// </summary>
public interface IConnectionType : IObjectType
{
    /// <summary>
    /// Gets the connection name of this connection type.
    /// </summary>
    string ConnectionName { get; }

    /// <summary>
    /// Gets the edge type of this connection.
    /// </summary>
    IEdgeType EdgeType { get; }
}
