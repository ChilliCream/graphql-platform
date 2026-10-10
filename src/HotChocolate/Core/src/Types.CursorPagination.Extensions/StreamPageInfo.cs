using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Information about pagination in a streaming connection.
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
public class StreamPageInfo<TNode>(StreamPage<TNode> page, int maxRelativeCursorCount) : PageInfo
{
    /// <inheritdoc />
    public override ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => page.HasNextPageAsync(cancellationToken);

    /// <inheritdoc />
    public override ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => page.HasPreviousPageAsync(cancellationToken);

    /// <inheritdoc />
    public override ValueTask<string?> GetStartCursorAsync(CancellationToken cancellationToken = default)
        => page.CreateStartCursorAsync(cancellationToken);

    /// <inheritdoc />
    public override ValueTask<string?> GetEndCursorAsync(CancellationToken cancellationToken = default)
        => page.CreateEndCursorAsync(cancellationToken);

    /// <inheritdoc />
    public override async ValueTask<IReadOnlyList<PageCursor>> GetForwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => await page.CreateRelativeForwardCursorsAsync(maxRelativeCursorCount, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public override async ValueTask<IReadOnlyList<PageCursor>> GetBackwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => await page.CreateRelativeBackwardCursorsAsync(maxRelativeCursorCount, cancellationToken)
            .ConfigureAwait(false);
}
