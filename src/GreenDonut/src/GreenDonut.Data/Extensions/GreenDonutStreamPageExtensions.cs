using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Extensions for the <see cref="StreamPage{T}"/> class.
/// </summary>
public static class GreenDonutStreamPageExtensions
{
    /// <summary>
    /// Creates a relative cursor for backwards pagination.
    /// </summary>
    /// <param name="page">
    /// The page to create cursors for.
    /// </param>
    /// <param name="maxCursors">
    /// The maximum number of cursors to create.
    /// </param>
    /// <returns>
    /// An array of cursors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the page is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the maximum number of cursors is less than 0.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the page does not allow relative cursors.
    /// </exception>
    /// <remarks>
    /// This method creates cursors for the previous pages based on the current page.
    /// The cursors are created using the <see cref="StreamPage{T}.CreateCursor(PageEntry{T}, int)"/> method.
    /// </remarks>
    public static ImmutableArray<PageCursor> CreateRelativeBackwardCursors<T>(
        this StreamPage<T> page,
        int maxCursors = 5)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (maxCursors < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCursors),
                "Max cursors must be greater than or equal to 0.");
        }

        if (page.BufferedCount == 0 || page.Index is null || page.Index == 1)
        {
            return [];
        }

        var firstEntry = page.GetBufferedEntry(0);
        var previousPages = page.Index.Value - 1;
        var cursors = ImmutableArray.CreateBuilder<PageCursor>();

        maxCursors *= -1;

        for (var i = 0; i > maxCursors && previousPages + i - 1 >= 0; i--)
        {
            cursors.Insert(
                0,
                new PageCursor(
                    page.CreateCursor(firstEntry, i),
                    previousPages + i));
        }

        return cursors.ToImmutable();
    }

    /// <summary>
    /// Creates a relative cursor for forwards pagination, reading ahead until the source completes.
    /// </summary>
    /// <param name="page">
    /// The page to create cursors for.
    /// </param>
    /// <param name="maxCursors">
    /// The maximum number of cursors to create.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// An array of cursors.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the page is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the maximum number of cursors is less than 0.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the page does not allow relative cursors.
    /// </exception>
    /// <remarks>
    /// This method creates cursors for the next pages based on the current page.
    /// The cursors are created using the <see cref="StreamPage{T}.CreateCursor(PageEntry{T}, int)"/> method.
    /// </remarks>
    public static async ValueTask<ImmutableArray<PageCursor>> CreateRelativeForwardCursorsAsync<T>(
        this StreamPage<T> page,
        int maxCursors = 5,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (maxCursors < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCursors),
                "Max cursors must be greater than or equal to 0.");
        }

        await page.DrainAsync(cancellationToken).ConfigureAwait(false);

        if (page.BufferedCount == 0 || page.Index is null)
        {
            return [];
        }

        var totalPages = Math.Ceiling((double)(page.TotalCount ?? 0) / (page.RequestedSize ?? 10));

        if (page.Index >= totalPages)
        {
            return [];
        }

        var lastEntry = page.GetBufferedEntry(page.BufferedCount - 1);
        var cursors = ImmutableArray.CreateBuilder<PageCursor>();
        cursors.Add(new PageCursor(page.CreateCursor(lastEntry, 0), page.Index.Value + 1));

        for (var i = 1; i < maxCursors && page.Index + i < totalPages; i++)
        {
            cursors.Add(
                new PageCursor(
                    page.CreateCursor(lastEntry, i),
                    page.Index.Value + i + 1));
        }

        return cursors.ToImmutable();
    }

    /// <summary>
    /// Creates a cursor for the last page of the dataset, or a page before it.
    /// </summary>
    /// <param name="page">
    /// The page to create the cursor for.
    /// </param>
    /// <param name="offset">
    /// The number of pages before the last page. Zero targets the last page itself.
    /// </param>
    /// <returns>
    /// Returns a cursor for the last page, offset backwards by <paramref name="offset"/> pages.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the page is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the offset is greater than zero, or if it moves the page number below the first page.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the page does not allow relative cursors.
    /// </exception>
    public static PageCursor CreateLastPageCursor<T>(this StreamPage<T> page, int offset = 0)
    {
        ArgumentNullException.ThrowIfNull(page);

        return LastPageCursorMath.CreateLastPageCursor(page.TotalCount, page.RequestedSize, offset);
    }

    /// <summary>
    /// Creates cursors for the pages between the current page and the last page of the dataset.
    /// </summary>
    /// <param name="page">
    /// The page to create cursors for.
    /// </param>
    /// <param name="maxCursors">
    /// The maximum number of cursors to create.
    /// </param>
    /// <returns>
    /// Returns an array of cursors, ordered by page number ascending, for the pages after the
    /// current page up to and including the last page. Empty if relative cursors are not available
    /// or the current page is already the last page.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the page is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the maximum number of cursors is less than 0.
    /// </exception>
    public static ImmutableArray<PageCursor> CreateRelativeLastPageCursors<T>(
        this StreamPage<T> page,
        int maxCursors = 5)
    {
        ArgumentNullException.ThrowIfNull(page);

        return LastPageCursorMath.CreateRelativeLastPageCursors(
            page.TotalCount,
            page.RequestedSize,
            page.Index,
            maxCursors);
    }
}
