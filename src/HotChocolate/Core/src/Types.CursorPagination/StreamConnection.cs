using System.Runtime.CompilerServices;

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
    public virtual IAsyncEnumerable<TNode>? Nodes => GetNodesAsync(Edges);

    /// <summary>
    /// Information to aid in pagination after the stream completes.
    /// </summary>
    [GraphQLDescription("Information to aid in pagination.")]
    public abstract Task<TPageInfo> PageInfo { get; }

    private static async IAsyncEnumerable<TNode> GetNodesAsync(
        IAsyncEnumerable<TEdge>? edges,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (edges is null)
        {
            yield break;
        }

        await foreach (var edge in edges.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return edge.Node;
        }
    }
}
