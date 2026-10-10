using System.Runtime.CompilerServices;
using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// A connection to a stream of items.
/// </summary>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Connection")]
[GraphQLDescription("A connection to a list of items.")]
public class StreamPageConnection<TNode>
{
    private readonly StreamPage<TNode> _page;
    private readonly int _maxRelativeCursorCount;
    private PageInfo? _pageInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPageConnection{TNode}"/> class.
    /// </summary>
    /// <param name="page">
    /// The page that contains the data.
    /// </param>
    /// <param name="maxRelativeCursorCount">
    /// The maximum number of relative cursors to create.
    /// </param>
    public StreamPageConnection(StreamPage<TNode> page, int maxRelativeCursorCount = 5)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRelativeCursorCount);

        _page = page;
        _maxRelativeCursorCount = maxRelativeCursorCount;
    }

    /// <summary>
    /// Streams the edges of the connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the enumeration.
    /// </param>
    [GraphQLDescription("A list of edges.")]
    public async IAsyncEnumerable<StreamPageEdge<TNode>>? GetEdgesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entry in _page.GetEntriesAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return new StreamPageEdge<TNode>(_page, entry);
        }
    }

    /// <summary>
    /// Streams a flattened list of the nodes.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the enumeration.
    /// </param>
    [GraphQLDescription("A flattened list of the nodes")]
    public async IAsyncEnumerable<TNode>? GetNodesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var node in _page.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return node;
        }
    }

    /// <summary>
    /// Information to aid in pagination.
    /// </summary>
    [GraphQLDescription("Information to aid in pagination.")]
    public PageInfo PageInfo
        => _pageInfo ??= new StreamPageInfo<TNode>(_page, _maxRelativeCursorCount);

    /// <summary>
    /// Gets the total count of items in the connection, or <c>null</c> if it was not requested.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token that cancels the operation.
    /// </param>
    [GraphQLDescription("Identifies the total count of items in the connection.")]
    [GraphQLType<NonNullType<IntType>>]
    public ValueTask<int?> GetTotalCountAsync(CancellationToken cancellationToken = default)
        => _page.TotalCountAsync(cancellationToken);

    /// <summary>
    /// Converts a <see cref="StreamPage{TNode}"/> to a <see cref="StreamPageConnection{TNode}"/>.
    /// </summary>
    /// <param name="page">
    /// The page to convert.
    /// </param>
    public static implicit operator StreamPageConnection<TNode>(StreamPage<TNode> page)
        => new(page);
}
