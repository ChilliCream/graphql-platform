namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents a connection whose edges are produced asynchronously.
/// </summary>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
/// <typeparam name="TEdge">
/// The type of the edge.
/// </typeparam>
/// <typeparam name="TPageInfo">
/// The type of the pagination information.
/// </typeparam>
public abstract class StreamConnection<TNode, TEdge, TPageInfo>
    where TEdge : IEdge<TNode>
    where TPageInfo : IPageInfo
{
    /// <summary>
    /// A stream of edges.
    /// </summary>
    [GraphQLDescription("A list of edges.")]
    public abstract IAsyncEnumerable<TEdge>? Edges { get; }

    /// <summary>
    /// A stream of nodes derived from the edges.
    /// </summary>
    [GraphQLDescription("A flattened list of the nodes")]
    public virtual IAsyncEnumerable<TNode>? Nodes => new NodesEnumerable(Edges);

    /// <summary>
    /// Information to aid in pagination after the stream completes.
    /// </summary>
    [GraphQLDescription("Information to aid in pagination.")]
    public abstract Task<TPageInfo> PageInfo { get; }

    private sealed class NodesEnumerable(IAsyncEnumerable<TEdge>? edges) : IAsyncEnumerable<TNode>
    {
        public IAsyncEnumerator<TNode> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => edges is { } source
                ? new NodesEnumerator(source.GetAsyncEnumerator(cancellationToken))
                : EmptyNodesEnumerator.Instance;
    }

    private sealed class NodesEnumerator(IAsyncEnumerator<TEdge> enumerator) : IAsyncEnumerator<TNode>
    {
        public TNode Current => enumerator.Current.Node;

        public ValueTask<bool> MoveNextAsync()
            => enumerator.MoveNextAsync();

        public ValueTask DisposeAsync()
            => enumerator.DisposeAsync();
    }

    private sealed class EmptyNodesEnumerator : IAsyncEnumerator<TNode>
    {
        public static readonly EmptyNodesEnumerator Instance = new();

        public TNode Current => default!;

        public ValueTask<bool> MoveNextAsync()
            => ValueTask.FromResult(false);

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
