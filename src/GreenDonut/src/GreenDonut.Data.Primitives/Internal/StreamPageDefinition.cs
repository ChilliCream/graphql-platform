namespace GreenDonut.Data.Internal;

/// <summary>
/// Describes how a streaming page turns raw rows from a <see cref="StreamPagePump{TElement}"/>
/// into buffered content, flags, and a total count.
/// </summary>
/// <typeparam name="TElement">
/// The type of the source rows.
/// </typeparam>
/// <param name="RequestedCount">
/// The requested number of items, used to recognize the forward trailing sentinel.
/// </param>
/// <param name="Forward">
/// Defines whether this page paginates forward from a cursor.
/// </param>
/// <param name="TrailingSentinel">
/// Defines whether the row after <paramref name="RequestedCount"/> content rows is read and
/// dropped to signal a next page, instead of being buffered as content.
/// </param>
/// <param name="SkipFront">
/// The number of rows to consume from the front of the source without buffering them.
/// </param>
/// <param name="SkipFrontFromCount">
/// Computes the number of rows to skip from the front once the total count is known, given the
/// total count and <paramref name="SkipFront"/>. Null when the skip count does not depend on the
/// total count.
/// </param>
/// <param name="Index">
/// The index number of this page, known synchronously at creation.
/// </param>
/// <param name="RequestedSize">
/// The requested page size, or null if unknown.
/// </param>
/// <param name="TotalCount">
/// The total count of items in the dataset, or null if it is not yet known.
/// </param>
/// <param name="HasNextPage">
/// Whether there is a next page, or null if it is not yet known.
/// </param>
/// <param name="HasPreviousPage">
/// Whether there is a previous page, or null if it is not yet known.
/// </param>
/// <param name="FlagsFromFirstRow">
/// Derives the next-page and previous-page flags from the first row read from the source. Null
/// when neither flag is derived from the first row.
/// </param>
internal readonly record struct StreamPageDefinition<TElement>(
    int RequestedCount,
    bool Forward,
    bool TrailingSentinel,
    int SkipFront,
    Func<int?, int, int>? SkipFrontFromCount,
    int? Index,
    int? RequestedSize,
    int? TotalCount,
    bool? HasNextPage,
    bool? HasPreviousPage,
    Func<StreamRow<TElement>, (bool? HasNextPage, bool? HasPreviousPage)>? FlagsFromFirstRow);
