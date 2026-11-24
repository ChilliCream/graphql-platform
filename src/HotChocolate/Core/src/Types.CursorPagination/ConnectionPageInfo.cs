using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the connection page info.
/// This class provides additional information about pagination in a connection.
/// </summary>
public class ConnectionPageInfo : PageInfo
{
    private readonly bool _hasNextPage;
    private readonly bool _hasPreviousPage;
    private readonly string? _startCursor;
    private readonly string? _endCursor;

    /// <summary>
    /// Initializes <see cref="ConnectionPageInfo" />.
    /// </summary>
    /// <param name="hasNextPage">Indicates whether more items exist after the current page.</param>
    /// <param name="hasPreviousPage">Indicates whether more items exist before the current page.</param>
    /// <param name="startCursor">The cursor of the first item in the current page.</param>
    /// <param name="endCursor">The cursor of the last item in the current page.</param>
    public ConnectionPageInfo(
        bool hasNextPage,
        bool hasPreviousPage,
        string? startCursor,
        string? endCursor)
    {
        _hasNextPage = hasNextPage;
        _hasPreviousPage = hasPreviousPage;
        _startCursor = startCursor;
        _endCursor = endCursor;
    }

    /// <summary>
    /// An empty page info without a next page, a previous page or cursors.
    /// </summary>
    public static ConnectionPageInfo Empty { get; } = new(false, false, null, null);

    /// <inheritdoc />
    public sealed override ValueTask<bool> HasNextPageAsync(CancellationToken cancellationToken = default)
        => new(_hasNextPage);

    /// <inheritdoc />
    public sealed override ValueTask<bool> HasPreviousPageAsync(CancellationToken cancellationToken = default)
        => new(_hasPreviousPage);

    /// <inheritdoc />
    public sealed override ValueTask<string?> GetStartCursorAsync(CancellationToken cancellationToken = default)
        => new(_startCursor);

    /// <inheritdoc />
    public sealed override ValueTask<string?> GetEndCursorAsync(CancellationToken cancellationToken = default)
        => new(_endCursor);

    /// <inheritdoc />
    public sealed override ValueTask<IReadOnlyList<PageCursor>> GetForwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => new(Array.Empty<PageCursor>());

    /// <inheritdoc />
    public sealed override ValueTask<IReadOnlyList<PageCursor>> GetBackwardCursorsAsync(
        CancellationToken cancellationToken = default)
        => new(Array.Empty<PageCursor>());
}
