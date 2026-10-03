namespace GreenDonut.Data.Internal;

/// <summary>
/// Creates the exceptions thrown by the entity framework paging APIs, so their messages live in
/// one place.
/// </summary>
internal static class ThrowHelper
{
    public static ArgumentException PagingArguments_EndCursorBeforeCombinedWithAfter()
        => new(
            "An end cursor in `before` cannot be combined with `after`.",
            "arguments");

    public static ArgumentException PagingArguments_EndCursorRequiresBeforeAndLast()
        => new(
            "An end cursor is only valid when used with `before` and `last`.",
            "arguments");

    public static ArgumentException PagingArguments_IncludeItemsFalseRequiresTotalCount()
        => new(
            "IncludeItems can only be false when the total count is included.",
            "arguments");

    public static ArgumentException PagingArguments_BeforeCombinedWithRelativeAfter()
        => new(
            "You cannot use `before` and `after` with relative cursors at the same time.",
            "arguments");

    public static ArgumentException PagingArguments_EndCursorOffsetOutOfRange()
        => new(
            "The end cursor offset points too far before the last page for the requested page size.",
            "arguments");

    public static ArgumentException PagingArguments_FirstAndLastBothSpecified()
        => new(
            "You can specify either `first` or `last`, but not both as this can lead to "
            + "unpredictable results.",
            "arguments");

    public static ArgumentException PagingArguments_ValueSelectorRequired()
        => new(
            "If no value selector is provided, the source element type must be assignable "
            + "to the value type.",
            "valueSelector");

    public static ArgumentException Paging_NoOrderByKeys()
        => new(
            "In order to use cursor pagination, you must specify at least one key using "
            + "the `OrderBy` method.",
            "keys");

    public static ArgumentException PagingArguments_RelativeOffsetDirectionMismatch()
        => new(
            "Positive offsets are not allowed with `last`, and negative offsets are not "
            + "allowed with `first`.",
            "arguments");

    public static ArgumentException PagingArguments_NullRequestedKey()
        => new(
            "The requested key set contains a null key.",
            "keySelector");

    public static ArgumentException PagingArguments_FirstMustBeGreaterThanZero()
        => new(
            "`first` must be greater than zero.",
            "arguments");

    public static ArgumentException PagingArguments_LastMustBeGreaterThanZero()
        => new(
            "`last` must be greater than zero.",
            "arguments");

    public static ArgumentException PagingArguments_RelativeOffsetOutOfRange()
        => new(
            "The relative cursor offset is too large for the requested page size.",
            "arguments");

    public static ArgumentException PagingArguments_BatchWindowTooLargeForPageSize()
        => new(
            "Twice the requested page size does not fit into an int, so the batch end cursor "
            + "window cannot be computed.",
            "arguments");
}
