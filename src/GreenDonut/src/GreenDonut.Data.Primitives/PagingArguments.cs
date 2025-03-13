namespace GreenDonut.Data;

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
    /// <param name="includeTotalCount">
    /// Defines if the total count of items in the dataset shall be included in the result.
    /// </param>
    public PagingArguments(
        int? first = null,
        string? after = null,
        int? last = null,
        string? before = null,
        bool includeTotalCount = false)
    {
        First = first;
        After = after;
        Last = last;
        Before = before;
        IncludeTotalCount = includeTotalCount;
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
    /// Defines if the total count of items in the dataset shall be included in the result.
    /// </summary>
    public bool IncludeTotalCount { get; init; }

    /// <summary>
    /// Defines if relative cursors are allowed.
    /// </summary>
    public bool EnableRelativeCursors { get; init; }

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
    /// <param name="includeTotalCount">
    /// Defines if the total count of items in the dataset shall be included in the result.
    /// </param>
    public void Deconstruct(
        out int? first,
        out string? after,
        out int? last,
        out string? before,
        out bool includeTotalCount)
    {
        first = First;
        after = After;
        last = Last;
        before = Before;
        includeTotalCount = IncludeTotalCount;
    }
}
