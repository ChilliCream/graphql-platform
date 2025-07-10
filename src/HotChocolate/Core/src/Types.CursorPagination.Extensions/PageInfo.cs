using GreenDonut.Data;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Information about pagination in a connection.
/// </summary>
[GraphQLDescription(
    "Information about pagination in a connection.")]
public abstract class PageInfo : IPageInfo
{
    /// <summary>
    /// Indicates whether more edges exist following
    /// the set defined by the clients arguments.
    /// </summary>
    [GraphQLDescription(
        "Indicates whether more edges exist following " +
        "the set defined by the clients arguments.")]
    public abstract bool HasNextPage { get; }

    /// <summary>
    /// Indicates whether more edges exist prior
    /// the set defined by the clients arguments.
    /// </summary>
    [GraphQLDescription(
        "Indicates whether more edges exist prior " +
        "the set defined by the clients arguments.")]
    public abstract bool HasPreviousPage { get; }

    /// <summary>
    /// When paginating backwards, the cursor to continue.
    /// </summary>
    [GraphQLDescription(
        "When paginating backwards, the cursor to continue.")]
    public abstract string? StartCursor { get; }

    /// <summary>
    /// When paginating forwards, the cursor to continue.
    /// </summary>
    [GraphQLDescription(
        "When paginating forwards, the cursor to continue.")]
    public abstract string? EndCursor { get; }

    /// <summary>
    /// A list of cursors to continue paginating forwards.
    /// </summary>
    [GraphQLDescription(
        "A list of cursors to continue paginating forwards.")]
    [GraphQLType<NonNullType<ListType<NonNullType<PageCursorType>>>>]
    public abstract IReadOnlyList<PageCursor> ForwardCursors { get; }

    /// <summary>
    /// A list of cursors to continue paginating backwards.
    /// </summary>
    [GraphQLDescription(
        "A list of cursors to continue paginating backwards.")]
    [GraphQLType<NonNullType<ListType<NonNullType<PageCursorType>>>>]
    public abstract IReadOnlyList<PageCursor> BackwardCursors { get; }
}

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
    public override bool HasNextPage => page.HasNextPage;

    /// <inheritdoc />
    public override bool HasPreviousPage => page.HasPreviousPage;

    /// <inheritdoc />
    public override string? StartCursor
        => page.First is not null
            ? page.CreateCursor(page.First)
            : null;

    /// <inheritdoc />
    public override string? EndCursor
        => page.Last is not null
            ? page.CreateCursor(page.Last)
            : null;

    /// <inheritdoc />
    public override IReadOnlyList<PageCursor> ForwardCursors
        => page.CreateRelativeForwardCursors(maxRelativeCursorCount);

    /// <inheritdoc />
    public override IReadOnlyList<PageCursor> BackwardCursors
        => page.CreateRelativeBackwardCursors(maxRelativeCursorCount);
}
