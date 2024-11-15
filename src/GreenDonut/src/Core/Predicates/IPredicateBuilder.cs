using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Predicates;

/// <summary>
/// The predicate builder helps you create a combined predicate expression
/// by adding multiple expressions together.
/// </summary>
[Experimental(Experiments.Predicates)]
public interface IPredicateBuilder
{
    /// <summary>
    /// Adds a predicate expression to the builder.
    /// </summary>
    /// <param name="selector">
    /// An expression that defines how to select data from the data source.
    /// </param>
    /// <typeparam name="T">
    /// The type of the data source.
    /// </typeparam>
    void Add<T>(Expression<Func<T, bool>> selector);

    /// <summary>
    /// Combines all the added predicate expressions into one.
    /// Returns null if no expressions were added.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the data source.
    /// </typeparam>
    /// <returns>
    /// A combined predicate expression, or null if none were added.
    /// </returns>
    Expression<Func<T, bool>>? TryCompile<T>();
}
