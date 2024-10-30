namespace HotChocolate.Pagination;

/// <summary>
/// The paging arguments are used to specify the paging behavior.
/// </summary>
public readonly record struct PagingArguments
{
    /// <summary>
    /// Initializes a new instance of <see cref="PagingArguments"/>.
    /// </summary>
    /// <param name="first">
    /// The number of entities that shall be taken from the beginning of the list.
    /// </param>
    /// <param name="after">
    /// The cursor after which entities shall be taken.
    /// </param>
    /// <param name="last">
    /// The number of entities that shall be taken from the end of the list.
    /// </param>
    /// <param name="before">
    /// The cursor before which entities shall be taken.
    /// </param>
    public PagingArguments(
        int? first = null,
        string? after = null,
        int? last = null,
        string? before = null)
    {
        First = first;
        After = after;
        Last = last;
        Before = before;
    }

    /// <summary>
    /// The number of entities that shall be taken from the beginning of the list.
    /// </summary>
    public int? First { get; init; }

    /// <summary>
    /// The cursor after which entities shall be taken.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// The number of entities that shall be taken from the end of the list.
    /// </summary>
    public int? Last { get; init; }

    /// <summary>
    /// The cursor before which entities shall be taken.
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Deconstructs the paging arguments into its components.
    /// </summary>
    /// <param name="first">
    /// The number of entities that shall be taken from the beginning of the list.
    /// </param>
    /// <param name="after">
    /// The cursor after which entities shall be taken.
    /// </param>
    /// <param name="last">
    /// The number of entities that shall be taken from the end of the list.
    /// </param>
    /// <param name="before">
    /// The cursor before which entities shall be taken.
    /// </param>
    public void Deconstruct(
        out int? first,
        out string? after,
        out int? last,
        out string? before)
    {
        first = First;
        after = After;
        last = Last;
        before = Before;
    }
}
