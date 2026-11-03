using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Information about pagination in a connection.
/// </summary>
/// <param name="page">
/// The page that contains the data.
/// </param>
/// <param name="maxRelativeCursorCount">
/// The maximum number of relative cursors to create.
/// </param>
/// <typeparam name="TNode">
/// The type of the node.
/// </typeparam>
public class PageInfo<TNode>(Page<TNode> page, int maxRelativeCursorCount = 5) : PageInfo
{
    /// <inheritdoc />
    public override ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => new(page.HasNextPage);

    /// <inheritdoc />
    public override ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => new(page.HasPreviousPage);

    /// <inheritdoc />
    public override ValueTask<string?> GetStartCursorAsync(CancellationToken cancellationToken = default)
        => new(page.CreateStartCursor());

    /// <inheritdoc />
    public override ValueTask<string?> GetEndCursorAsync(CancellationToken cancellationToken = default)
        => new(page.CreateEndCursor());

    /// <inheritdoc />
    public override ValueTask<IReadOnlyList<PageCursor>> GetForwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => new(page.CreateRelativeForwardCursors(maxRelativeCursorCount));

    /// <inheritdoc />
    public override ValueTask<IReadOnlyList<PageCursor>> GetBackwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => new(page.CreateRelativeBackwardCursors(maxRelativeCursorCount));
}
