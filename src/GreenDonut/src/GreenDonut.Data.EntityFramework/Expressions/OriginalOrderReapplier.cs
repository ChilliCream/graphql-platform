using System.Reflection;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// Re-applies a dataset's declared ordering on top of an already limited queryable. Used by
/// backward streaming pages, which slice the dataset in reverse order and then need the
/// resulting rows back in their original, ascending order before they are streamed out.
/// </summary>
internal static class OriginalOrderReapplier
{
    private static readonly MethodInfo s_orderByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_orderByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_thenByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_thenByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Length == 2);

    /// <summary>
    /// Applies an order-by chain built from <paramref name="keys"/>, in the same sequence and
    /// direction as the original dataset, on top of <paramref name="query"/>.
    /// </summary>
    /// <param name="query">
    /// The queryable to order, typically one already limited by a preceding <c>Skip</c> and
    /// <c>Take</c> in reverse order.
    /// </param>
    /// <param name="keys">
    /// The keys that define the original order, in declaration order.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns the queryable, reordered ascending relative to each key's original direction.
    /// </returns>
    public static IQueryable<T> Reapply<T>(IQueryable<T> query, ReadOnlySpan<CursorKey> keys)
    {
        var current = query;

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var ascending = key.Direction == CursorKeyDirection.Ascending;
            var method = i == 0
                ? ascending ? s_orderByMethod : s_orderByDescendingMethod
                : ascending ? s_thenByMethod : s_thenByDescendingMethod;

            var generic = method.MakeGenericMethod(typeof(T), key.Expression.ReturnType);
            current = (IQueryable<T>)generic.Invoke(null, [current, key.Expression])!;
        }

        return current;
    }
}
