namespace GreenDonut.Data.Internal;

/// <summary>
/// Creates the exceptions thrown by the cursor and streaming page APIs, so their messages live in
/// one place.
/// </summary>
internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException EndCursor_OffsetMustNotBeGreaterThanZero(int offset)
        => new(
            nameof(offset),
            offset,
            "The offset of an end cursor must not be greater than zero.");

    public static InvalidOperationException CursorParser_PageInfoCouldNotBeParsed()
        => new("The cursor page info could not be parsed.");

    public static ArgumentOutOfRangeException LastPageCursor_OffsetMustNotBeGreaterThanZero(int offset)
        => new(
            nameof(offset),
            offset,
            "The offset of a last page cursor must not be greater than zero.");

    public static InvalidOperationException LastPageCursor_RelativeCursorsNotAllowed()
        => new("This page does not allow relative cursors.");

    public static ArgumentOutOfRangeException LastPageCursor_OffsetMovesBeforeFirstPage(int offset)
        => new(
            nameof(offset),
            offset,
            "The offset moves the page number before the first page.");

    public static ArgumentOutOfRangeException RelativeCursors_MaxCursorsMustNotBeNegative(int maxCursors)
        => new(
            nameof(maxCursors),
            "Max cursors must be greater than or equal to 0.");

    public static InvalidOperationException PagingArgumentsHash_BufferTooSmall()
        => new("Buffer is too small.");
}
