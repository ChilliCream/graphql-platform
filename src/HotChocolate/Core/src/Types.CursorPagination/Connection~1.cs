using System.Buffers;

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
        IReadOnlyList<IEdge<T>> edges,
        ConnectionPageInfo info,
        int totalCount = 0)
        : base(edges, info, totalCount)
    {
        Edges = edges;
    }

    public Connection(
        ConnectionPageInfo info,
        int totalCount = 0)
        : base([], info, totalCount)
    {
        Edges = [];
    }

    /// <summary>
    /// The edges that belong to this connection.
    /// </summary>
    public new IReadOnlyList<IEdge<T>> Edges { get; }

    /// <inheritdoc cref="Connection"/>
    public override void Accept(IPageObserver observer)
    {
        if (Edges.Count == 0)
        {
            ReadOnlySpan<T> empty = [];
            observer.OnAfterSliced(empty, Info);
            return;
        }

        var buffer = ArrayPool<T>.Shared.Rent(Edges.Count);

        for (var i = 0; i < Edges.Count; i++)
        {
            buffer[i] = Edges[i].Node;
        }

        ReadOnlySpan<T> items = buffer.AsSpan(0, Edges.Count);
        observer.OnAfterSliced(items, Info);

        buffer.AsSpan()[..Edges.Count].Clear();
        ArrayPool<T>.Shared.Return(buffer);
    }
}
