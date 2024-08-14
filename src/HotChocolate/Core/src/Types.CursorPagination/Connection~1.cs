namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public class Connection<T> : Connection
{
    /// <summary>
    /// Initializes <see cref="Connection{T}" />.
    /// </summary>
    /// <param name="edges">
    /// The edges that belong to this connection.
    /// </param>
    /// <param name="info">
    /// Additional information about this connection.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items of this connection
    /// </param>
    public Connection(
        IReadOnlyCollection<Edge<T>> edges,
        ConnectionPageInfo info,
        int totalCount = 0)
        : base(edges, info, totalCount)
    {
        Edges = edges;
    }

    public Connection(
        ConnectionPageInfo info,
        int totalCount = 0)
        : base(Array.Empty<Edge<T>>(), info, totalCount)
    {
        Edges = Array.Empty<Edge<T>>();
    }

    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    public new IReadOnlyCollection<Edge<T>> Edges { get; }
}
