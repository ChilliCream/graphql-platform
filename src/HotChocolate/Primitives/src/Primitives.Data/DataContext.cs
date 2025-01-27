using System.Linq.Expressions;

namespace HotChocolate.Data;

public record DataContext<TEntity>(
    Expression<Func<TEntity, TEntity>>? Selector = null,
    Expression<Func<TEntity, bool>>? Predicate = null,
    SortDefinition<TEntity>? Sorting = null);
