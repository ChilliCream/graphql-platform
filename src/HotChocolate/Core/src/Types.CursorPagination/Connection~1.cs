using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="getTotalCount">
    /// A delegate to request the the total count.
    /// </param>
    public Connection(
        IReadOnlyCollection<Edge<T>> edges,
        ConnectionPageInfo info,
        Func<CancellationToken, ValueTask<int>> getTotalCount)
        : base(edges, info, getTotalCount)
    {
        Edges = edges;
    }

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

    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    public new IReadOnlyCollection<Edge<T>> Edges { get; }
}
