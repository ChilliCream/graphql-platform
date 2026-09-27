using System.Collections.Immutable;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data;

/// <summary>
/// Shared arithmetic for the last-page cursor helpers of <see cref="Page{T}"/> and
/// <see cref="StreamPage{T}"/>.
/// </summary>
internal static class LastPageCursorMath
{
    /// <summary>
    /// Computes the one-based number of the last page for a dataset.
    /// </summary>
    /// <param name="totalCount">
    /// The total number of items in the dataset.
    /// </param>
    /// <param name="requestedSize">
    /// The requested page size.
    /// </param>
    private static int GetLastPageNumber(int totalCount, int requestedSize)
        => Math.Max(1, (int)Math.Ceiling((double)totalCount / requestedSize));

    /// <summary>
    /// Creates a cursor for the last page of the dataset, or a page before it.
    /// </summary>
    /// <param name="totalCount">
    /// The total number of items in the dataset.
    /// </param>
    /// <param name="requestedSize">
    /// The requested page size.
    /// </param>
    /// <param name="offset">
    /// The number of pages before the last page. Zero targets the last page itself.
    /// </param>
    /// <returns>
    /// Returns a cursor for the last page, offset backwards by <paramref name="offset"/> pages.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the offset is greater than zero, or if it moves the page number below the first page.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the page does not allow relative cursors.
    /// </exception>
    internal static PageCursor CreateLastPageCursor(int? totalCount, int? requestedSize, int offset)
    {
        if (offset > 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "The offset of a last page cursor must not be greater than zero.");
        }

        if (totalCount is null || requestedSize is null)
        {
            throw new InvalidOperationException("This page does not allow relative cursors.");
        }

        var lastPageNumber = GetLastPageNumber(totalCount.Value, requestedSize.Value);
        var pageNumber = lastPageNumber + offset;

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "The offset moves the page number before the first page.");
        }

        return new PageCursor(CursorFormatter.FormatEndCursor(offset, totalCount.Value), pageNumber);
    }

    /// <summary>
    /// Creates cursors for the pages between the current page and the last page of the dataset.
    /// </summary>
    /// <param name="totalCount">
    /// The total number of items in the dataset.
    /// </param>
    /// <param name="requestedSize">
    /// The requested page size.
    /// </param>
    /// <param name="index">
    /// The one-based number of the current page.
    /// </param>
    /// <param name="maxCursors">
    /// The maximum number of cursors to create.
    /// </param>
    /// <returns>
    /// Returns an array of cursors, ordered by page number ascending, for the pages after the
    /// current page up to and including the last page. Empty if relative cursors are not available
    /// or the current page is already the last page.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the maximum number of cursors is less than 0.
    /// </exception>
    internal static ImmutableArray<PageCursor> CreateRelativeLastPageCursors(
        int? totalCount,
        int? requestedSize,
        int? index,
        int maxCursors)
    {
        if (maxCursors < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCursors),
                "Max cursors must be greater than or equal to 0.");
        }

        if (totalCount is null || requestedSize is null || index is null)
        {
            return [];
        }

        var lastPageNumber = GetLastPageNumber(totalCount.Value, requestedSize.Value);

        if (lastPageNumber <= index)
        {
            return [];
        }

        var totalCountValue = totalCount.Value;
        var cursors = ImmutableArray.CreateBuilder<PageCursor>();

        for (var offset = 0; offset > -maxCursors; offset--)
        {
            var pageNumber = lastPageNumber + offset;

            if (pageNumber <= index || pageNumber < 1)
            {
                break;
            }

            cursors.Insert(0, new PageCursor(CursorFormatter.FormatEndCursor(offset, totalCountValue), pageNumber));
        }

        return cursors.ToImmutable();
    }
}
