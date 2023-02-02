using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public class Connection : IPage
{
    private readonly Func<CancellationToken, ValueTask<int>> _getTotalCount;

    /// <summary>
    /// Initializes <see cref="Connection" />.
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
        IReadOnlyCollection<IEdge> edges,
        ConnectionPageInfo info,
        Func<CancellationToken, ValueTask<int>> getTotalCount)
    {
        _getTotalCount = getTotalCount ??
            throw new ArgumentNullException(nameof(getTotalCount));
        Edges = edges ??
            throw new ArgumentNullException(nameof(edges));
        Info = info ??
            throw new ArgumentNullException(nameof(info));
    }

    /// <summary>
    /// Initializes <see cref="Connection" />.
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
        IReadOnlyCollection<IEdge> edges,
        ConnectionPageInfo info,
        int totalCount = 0)
    {
        _getTotalCount = _ => new(totalCount);
        Edges = edges ??
            throw new ArgumentNullException(nameof(edges));
        Info = info ??
            throw new ArgumentNullException(nameof(info));
    }

    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    public IReadOnlyCollection<IEdge> Edges { get; }

    /// <summary>
    /// The items that belong to this connection.
    /// </summary>
    IReadOnlyCollection<object> IPage.Items => Edges;

    /// <summary>
    /// Information about pagination in a connection.
    /// </summary>
    public ConnectionPageInfo Info { get; }

    /// <summary>
    /// Information about pagination in a connection.
    /// </summary>
    IPageInfo IPage.Info => Info;

    /// <summary>
    /// Requests the total count of the data set / collection that is being paged.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The total count of the data set / collection.
    /// </returns>
    public ValueTask<int> GetTotalCountAsync(CancellationToken cancellationToken) =>
        _getTotalCount(cancellationToken);
}
