using System.Buffers;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The connection represents one section of a dataset / collection.
/// </summary>
public abstract class ConnectionBase<TNode, TEdge, TPageInfo>
    : IConnection<TNode>
    where TEdge : IEdge<TNode>
    where TPageInfo : IPageInfo
{
    public abstract IReadOnlyList<TEdge>? Edges { get; }

    public abstract TPageInfo PageInfo { get; }

    IReadOnlyList<IEdge<TNode>>? IConnection<TNode>.Edges => (IReadOnlyList<IEdge<TNode>>?)Edges;

    IReadOnlyList<IEdge>? IConnection.Edges => (IReadOnlyList<IEdge<TNode>>?)Edges;

    IReadOnlyList<object> IPage.Items => (IReadOnlyList<IEdge<TNode>>?)Edges!;

    IPageInfo IPage.Info => PageInfo;

    void IPage.Accept(IPageObserver observer)
    {
        if (Edges is null || Edges.Count == 0)
        {
            ReadOnlySpan<TNode> empty = [];
            observer.OnAfterSliced(empty, PageInfo);
            return;
        }

        var buffer = ArrayPool<TNode>.Shared.Rent(Edges.Count);

        for (var i = 0; i < Edges.Count; i++)
        {
            buffer[i] = Edges[i].Node;
        }

        ReadOnlySpan<TNode> items = buffer.AsSpan(0, Edges.Count);
        observer.OnAfterSliced(items, PageInfo);

        buffer.AsSpan()[..Edges.Count].Clear();
        ArrayPool<TNode>.Shared.Return(buffer);
    }
}
