#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Projections;

/// <summary>
/// Represents a query that has the selector expression
/// applied and need a key selector.
/// </summary>
/// <typeparam name="T">
/// The entity type.
/// </typeparam>
[Experimental(Experiments.Projections)]
public interface ISelectorQuery<T>
{
    /// <summary>
    /// Selects the key of the entity in context of a DataLoader
    /// to guarantee that the query will load
    /// the DataLoader key values.
    /// </summary>
    /// <param name="key">
    /// The key selector expression.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IQueryable{T}"/> with the data selector
    /// and key selector applied to it.
    /// </returns>
    IQueryable<T> SelectKey(Expression<Func<T, object>> key);
}
#endif
