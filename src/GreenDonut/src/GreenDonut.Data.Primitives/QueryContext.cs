using System.Linq.Expressions;

namespace GreenDonut.Data;

/// <summary>
/// Represents the context for constructing queries against a data source for <typeparamref name="TEntity"/>.
/// This record contains the projection (selector), filtering (predicate), and sorting instructions
/// (<see cref="SortDefinition{TEntity}"/>) for building a query.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type associated with the query context.
/// </typeparam>
/// <param name="Selector">
/// An expression that defines what data shall be selected from <typeparamref name="TEntity"/>.
/// </param>
/// <param name="Predicate">
/// An expression that defines the filtering condition for <typeparamref name="TEntity"/>.
/// </param>
/// <param name="Sorting">
/// The sorting instructions (see <see cref="SortDefinition{TEntity}"/>) for <typeparamref name="TEntity"/>.
/// </param>
public record QueryContext<TEntity>(
    Expression<Func<TEntity, TEntity>>? Selector = null,
    Expression<Func<TEntity, bool>>? Predicate = null,
    SortDefinition<TEntity>? Sorting = null)
{
    /// <summary>
    /// An empty query context.
    /// </summary>
    public static QueryContext<TEntity> Empty { get; } = new();
}
