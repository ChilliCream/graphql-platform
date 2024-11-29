namespace HotChocolate.Types.Pagination;

/// <summary>
/// The cursor paging arguments are used to specify the
/// paging behavior of the cursor based paging handler.
/// </summary>
public readonly record struct CursorPagingArguments
{
    /// <summary>
    /// Initializes a new instance of <see cref="CursorPagingArguments"/>.
    /// </summary>
    /// <param name="first">
    /// The number of entities that shall be taken from the beginning of the list.
    /// </param>
    /// <param name="last">
    /// The number of entities that shall be taken from the end of the list.
    /// </param>
    /// <param name="after">
    /// The cursor after which entities shall be taken.
    /// </param>
    /// <param name="before">
    /// The cursor before which entities shall be taken.
    /// </param>
    public CursorPagingArguments(
        int? first = null,
        int? last = null,
        string? after = null,
        string? before = null)
    {
        First = first;
        Last = last;
        After = after;
        Before = before;
    }

    /// <summary>
    /// The number of entities that shall be taken from the beginning of the list.
    /// </summary>
    public int? First { get; }

    /// <summary>
    /// The number of entities that shall be taken from the end of the list.
    /// </summary>
    public int? Last { get; }

    /// <summary>
    /// The cursor after which entities shall be taken.
    /// </summary>
    public string? After { get; }

    /// <summary>
    /// The cursor before which entities shall be taken.
    /// </summary>
    public string? Before { get; }
}
