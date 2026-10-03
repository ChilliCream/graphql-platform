using System.Linq.Expressions;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// The flat, key-ordered query behind <c>ToBatchStreamPageAsync</c>, together with the
/// slicing decisions the caller needs to turn its rows into pages.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key that routes a row to its page.
/// </typeparam>
/// <typeparam name="TElement">
/// The type of the source rows.
/// </typeparam>
internal readonly struct BatchStreamQuery<TKey, TElement>(
    IQueryable<StreamBatchRow<TKey, TElement>> flatQuery,
    bool isBackward,
    Cursor? cursor,
    Expression<Func<TElement, bool>>? afterPredicate,
    Expression<Func<TElement, bool>>? beforePredicate,
    int skipAmount)
    where TKey : notnull
{
    /// <summary>
    /// The flat, explicitly key-ordered query, one row per requested key's content or
    /// trailing sentinel.
    /// </summary>
    public IQueryable<StreamBatchRow<TKey, TElement>> FlatQuery { get; } = flatQuery;

    /// <summary>
    /// Whether every key's page paginates backward (<c>last</c>).
    /// </summary>
    public bool IsBackward { get; } = isBackward;

    /// <summary>
    /// The parsed cursor, or null if neither <c>after</c> nor <c>before</c> was specified.
    /// </summary>
    public Cursor? Cursor { get; } = cursor;

    /// <summary>
    /// The predicate a relative forward cursor page's per-key emptiness is checked against,
    /// or null if no <c>after</c> cursor was given.
    /// </summary>
    public Expression<Func<TElement, bool>>? AfterPredicate { get; } = afterPredicate;

    /// <summary>
    /// The predicate a plain backward page's per-key "more before this page" flag is counted
    /// against, and a relative backward cursor page's per-key emptiness is checked against,
    /// or null if no <c>before</c> cursor was given.
    /// </summary>
    public Expression<Func<TElement, bool>>? BeforePredicate { get; } = beforePredicate;

    /// <summary>
    /// The number of rows skipped from the front of every key's correlated window, derived
    /// from the cursor's offset, if any, and the requested page size.
    /// </summary>
    public int SkipAmount { get; } = skipAmount;
}
