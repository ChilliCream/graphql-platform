using System.Linq.Expressions;

namespace HotChocolate.Data;

/// <summary>
/// Represents a sort operation on a field.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type on which the sort operation is applied.
/// </typeparam>
public interface ISortBy<TEntity>
{
    /// <summary>
    /// Gets the field on which the sort operation is applied.
    /// </summary>
    Expression KeySelector { get; }

    /// <summary>
    /// Gets the sort direction.
    /// </summary>
    bool Ascending { get; }

    /// <summary>
    /// Applies the sort operation to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable to which the sort operation is applied.
    /// </param>
    /// <returns>
    /// The queryable with the sort operation applied.
    /// </returns>
    IOrderedQueryable<TEntity> ApplyOrderBy(IQueryable<TEntity> queryable);

    /// <summary>
    /// Applies the sort operation to the queryable.
    /// </summary>
    /// <param name="queryable">
    /// The queryable to which the sort operation is applied.
    /// </param>
    /// <returns>
    /// The queryable with the sort operation applied.
    /// </returns>
    IOrderedQueryable<TEntity> ApplyThenBy(IOrderedQueryable<TEntity> queryable);
}
