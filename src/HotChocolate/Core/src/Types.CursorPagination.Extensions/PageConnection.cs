using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// A connection to a list of items.
/// </summary>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
[GraphQLName("{0}Connection")]
[GraphQLDescription("A connection to a list of items.")]
public class PageConnection<TNode> : ConnectionBase<TNode, PageEdge<TNode>, PageInfo>
{
    private readonly Page<TNode> _page;
    private readonly int _maxRelativeCursorCount;
    private PageEdge<TNode>[]? _edges;
    private PageInfo? _pageInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="PageConnection{TNode}"/> class.
    /// </summary>
    /// <param name="page">
    /// The page that contains the data.
    /// </param>
    /// <param name="maxRelativeCursorCount">
    /// The maximum number of relative cursors to create.
    /// </param>
    public PageConnection(Page<TNode> page, int maxRelativeCursorCount = 5)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRelativeCursorCount);

        _page = page;
        _maxRelativeCursorCount = maxRelativeCursorCount;
    }

    /// <summary>
    /// A list of edges.
    /// </summary>
    [GraphQLDescription("A list of edges.")]
    public override IReadOnlyList<PageEdge<TNode>>? Edges
    {
        get
        {
            if (_edges is null)
            {
                var items = _page.Items;
                var edges = new PageEdge<TNode>[items.Length];

                for (var i = 0; i < items.Length; i++)
                {
                    edges[i] = new PageEdge<TNode>(_page, items[i]);
                }

                _edges = edges;
            }

            return _edges;
        }
    }

    /// <summary>
    /// A flattened list of the nodes.
    /// </summary>
    [GraphQLDescription("A flattened list of the nodes")]
    public virtual IReadOnlyList<TNode>? Nodes => _page.Items;

    /// <summary>
    /// Information to aid in pagination.
    /// </summary>
    [GraphQLDescription("Information to aid in pagination.")]
    public override PageInfo PageInfo => _pageInfo ??= new PageInfo<TNode>(_page, _maxRelativeCursorCount);

    /// <summary>
    /// Identifies the total count of items in the connection.
    /// </summary>
    [GraphQLDescription("Identifies the total count of items in the connection.")]
    public int TotalCount => _page.TotalCount ?? -1;
}
