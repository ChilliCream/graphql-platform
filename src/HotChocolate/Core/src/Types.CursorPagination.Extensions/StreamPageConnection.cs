using System.Runtime.CompilerServices;
using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// A connection to a streamed list of items.
/// </summary>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Connection")]
[GraphQLDescription("A connection to a list of items.")]
public class StreamPageConnection<TNode>
    : StreamConnection<TNode, StreamEdge<TNode>, PageInfo>
{
    private readonly StreamPage<TNode> _page;
    private readonly IAsyncEnumerable<StreamEdge<TNode>> _edges;
    private Task<PageInfo>? _pageInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPageConnection{TNode}"/> class.
    /// </summary>
    /// <param name="page">
    /// The streamed page that contains the data.
    /// </param>
    public StreamPageConnection(StreamPage<TNode> page)
    {
        ArgumentNullException.ThrowIfNull(page);

        _page = page;
        _edges = GetEdgesAsync(page);
    }

    /// <summary>
    /// A stream of edges.
    /// </summary>
    [GraphQLDescription("A list of edges.")]
    public override IAsyncEnumerable<StreamEdge<TNode>>? Edges => _edges;

    /// <summary>
    /// A flattened stream of the nodes.
    /// </summary>
    [GraphQLDescription("A flattened list of the nodes")]
    public override IAsyncEnumerable<TNode>? Nodes => base.Nodes;

    /// <summary>
    /// Information to aid in pagination after the stream completes.
    /// </summary>
    [GraphQLDescription("Information to aid in pagination.")]
    public override Task<PageInfo> PageInfo => _pageInfo ??= GetPageInfoAsync(_page);

    /// <summary>
    /// Identifies the total count of items in the connection.
    /// </summary>
    [GraphQLDescription("Identifies the total count of items in the connection.")]
    [GraphQLType<NonNullType<IntType>>]
    public int? TotalCount => _page.TotalCount;

    /// <summary>
    /// Converts a <see cref="StreamPage{TNode}"/> to a <see cref="StreamPageConnection{TNode}"/>.
    /// </summary>
    /// <param name="page">
    /// The streamed page to convert.
    /// </param>
    public static implicit operator StreamPageConnection<TNode>(StreamPage<TNode> page)
        => new(page);

    private static async IAsyncEnumerable<StreamEdge<TNode>> GetEdgesAsync(
        StreamPage<TNode> page,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var edge in page.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return new StreamEdge<TNode>(edge.Node, edge.Cursor);
        }
    }

    private static async Task<PageInfo> GetPageInfoAsync(StreamPage<TNode> page)
        => new StreamPageInfo(await page.Completion.ConfigureAwait(false));
}
