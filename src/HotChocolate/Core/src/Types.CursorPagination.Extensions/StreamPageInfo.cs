using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Information about a streamed page after its enumeration completes.
/// </summary>
internal sealed class StreamPageInfo(StreamPageCompletion completion) : PageInfo
{
    /// <inheritdoc />
    public override bool HasNextPage => completion.HasNextPage;

    /// <inheritdoc />
    public override bool HasPreviousPage => completion.HasPreviousPage;

    /// <inheritdoc />
    public override string? StartCursor => completion.StartCursor;

    /// <inheritdoc />
    public override string? EndCursor => completion.EndCursor;

    /// <inheritdoc />
    public override IReadOnlyList<PageCursor> ForwardCursors => Array.Empty<PageCursor>();

    /// <inheritdoc />
    public override IReadOnlyList<PageCursor> BackwardCursors => Array.Empty<PageCursor>();
}
