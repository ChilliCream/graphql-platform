using System.Linq.Expressions;

namespace GreenDonut.Data;

/// <summary>
/// Represents a sort operation on a field.
/// </summary>
public sealed class SortBy<TEntity, TValue> : ISortBy<TEntity>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SortBy{TEntity, TValue}"/>.
    /// </summary>
    /// <param name="keySelector">
    /// The field on which the sort operation is applied.
    /// </param>
    /// <param name="ascending">
    /// Specifies the sort directive.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="keySelector"/> is <c>null</c>.
    /// </exception>
    public SortBy(Expression<Func<TEntity, TValue>> keySelector, bool ascending = true)
    {
        KeySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        Ascending = ascending;
    }

    /// <summary>
    /// Gets the field on which the sort operation is applied.
    /// </summary>
    public Expression<Func<TEntity, TValue>> KeySelector { get; init; }

    LambdaExpression ISortBy<TEntity>.KeySelector => KeySelector;

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    public bool Ascending { get; init; }

    /// <summary>
    /// Applies the sort operation to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable to which the sort operation is applied.
    /// </param>
    /// <returns>
    /// The queryable with the sort operation applied.
    /// </returns>
    public IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> queryable)
    {
        ArgumentNullException.ThrowIfNull(queryable);

        if (Ascending)
        {
            return queryable.OrderBy(KeySelector);
        }

        return queryable.OrderByDescending(KeySelector);
    }

    /// <summary>
    /// Applies the sort operation to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable to which the sort operation is applied.
    /// </param>
    /// <returns>
    /// The queryable with the sort operation applied.
    /// </returns>
    public IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> queryable)
    {
        if (Ascending)
        {
            return queryable.ThenBy(KeySelector);
        }

        return queryable.ThenByDescending(KeySelector);
    }
}

/// <summary>
/// Provides factory methods for creating sort operations.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type associated with the sort operation.
/// </typeparam>
public static class SortBy<TEntity>
{
    /// <summary>
    /// Creates a sort operation that sorts in ascending order.
    /// </summary>
    /// <param name="keySelector">
    /// The field on which the sort operation is applied.
    /// </param>
    /// <typeparam name="TValue">
    /// The type of the field on which the sort operation is applied.
    /// </typeparam>
    /// <returns>
    /// A sort operation that sorts in ascending order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="keySelector"/> is <c>null</c>.
    /// </exception>
    public static SortBy<TEntity, TValue> Ascending<TValue>(
        Expression<Func<TEntity, TValue>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        return new SortBy<TEntity, TValue>(keySelector, true);
    }

    /// <summary>
    /// Creates a sort operation that sorts in descending order.
    /// </summary>
    /// <param name="keySelector">
    /// The field on which the sort operation is applied.
    /// </param>
    /// <typeparam name="TValue">
    /// The type of the field on which the sort operation is applied.
    /// </typeparam>
    /// <returns>
    /// A sort operation that sorts in descending order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="keySelector"/> is <c>null</c>.
    /// </exception>
    public static SortBy<TEntity, TValue> Descending<TValue>(
        Expression<Func<TEntity, TValue>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        return new SortBy<TEntity, TValue>(keySelector, false);
    }
}
