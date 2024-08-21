using HotChocolate.Types.Pagination.Utilities;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public class Connection : IPage
{
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
        IReadOnlyList<IEdge> edges,
        ConnectionPageInfo info,
        int totalCount = 0)
    {
        Edges = edges ?? throw new ArgumentNullException(nameof(edges));
        Info = info ?? throw new ArgumentNullException(nameof(info));
        TotalCount = totalCount;
    }

    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    public IReadOnlyList<IEdge> Edges { get; }

    /// <summary>
    /// The items that belong to this connection.
    /// </summary>
    IReadOnlyList<object> IPage.Items => Edges;

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
    /// <returns>
    /// The total count of the data set / collection.
    /// </returns>
    public int TotalCount { get; }

    /// <summary>
    /// Accepts a page observer.
    /// </summary>
    public virtual void Accept(IPageObserver observer)
    {
    }

    /// <summary>
    /// Gets an cashed empty connection object.
    /// </summary>
    public static Connection Empty() => EmptyConnectionHolder.Empty;

    /// <summary>
    /// Gets an cashed empty connection object.
    /// </summary>
    public static Connection<T> Empty<T>() => EmptyConnectionHolder<T>.Empty;
}
