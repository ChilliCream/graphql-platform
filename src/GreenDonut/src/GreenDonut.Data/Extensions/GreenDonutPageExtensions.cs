using System.Collections.Immutable;

namespace GreenDonut.Data;

/// <summary>
/// Extensions for the <see cref="Page{T}"/> class.
/// </summary>
public static class GreenDonutPageExtensions
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
    /// The cursors are created using the <see cref="Page{T}.CreateCursor(T, int)"/> method.
    /// </remarks>
    public static ImmutableArray<PageCursor> CreateRelativeBackwardCursors<T>(this Page<T> page, int maxCursors = 5)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (maxCursors < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCursors),
                "Max cursors must be greater than or equal to 0.");
        }

        if (page.First is null || page.Index is null || page.Index == 1)
        {
            return [];
        }

        var previousPages = page.Index.Value - 1;
        var cursors = ImmutableArray.CreateBuilder<PageCursor>();

        maxCursors *= -1;

        for (var i = 0; i > maxCursors && previousPages + i - 1 >= 0; i--)
        {
            cursors.Insert(
                0,
                new PageCursor(
                    page.CreateCursor(page.First, i),
                    previousPages + i));
        }

        return cursors.ToImmutable();
    }

    /// <summary>
    /// Creates a relative cursor for forwards pagination.
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
    /// This method creates cursors for the next pages based on the current page.
    /// The cursors are created using the <see cref="Page{T}.CreateCursor(T, int)"/> method.
    /// </remarks>
    public static ImmutableArray<PageCursor> CreateRelativeForwardCursors<T>(this Page<T> page, int maxCursors = 5)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (maxCursors < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCursors),
                "Max cursors must be greater than or equal to 0.");
        }

        if (page.Last is null || page.Index is null)
        {
            return [];
        }

        var totalPages = Math.Ceiling((double)(page.TotalCount ?? 0) / (page.RequestedSize ?? 10));

        if (page.Index >= totalPages)
        {
            return [];
        }

        var cursors = ImmutableArray.CreateBuilder<PageCursor>();
        cursors.Add(new PageCursor(page.CreateCursor(page.Last, 0), page.Index.Value + 1));

        for (var i = 1; i < maxCursors && page.Index + i < totalPages; i++)
        {
            cursors.Add(
                new PageCursor(
                    page.CreateCursor(page.Last, i),
                    page.Index.Value + i + 1));
        }

        return cursors.ToImmutable();
    }
}
