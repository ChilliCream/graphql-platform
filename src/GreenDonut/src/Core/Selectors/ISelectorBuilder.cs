using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace GreenDonut.Selectors;

/// <summary>
/// The selector builder allows to build up a selector expression
/// by adding expressions that will be merged into a
/// single selector expression.
/// </summary>
[Experimental(Experiments.Selectors)]
public interface ISelectorBuilder
{
    /// <summary>
    /// Adds a selector expression to the builder.
    /// </summary>
    /// <param name="selector">
    /// A selector expression that specifies
    /// what data shall be fetched from
    /// the data source.
    /// </param>
    /// <typeparam name="T">
    /// The type of the data source.
    /// </typeparam>
    void Add<T>(Expression<Func<T, T>> selector);

    /// <summary>
    /// Merges all added selector expressions into a single
    /// selector expression that is applied to a query.
    /// If null is returned that no selector was ever added.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the data source.
    /// </typeparam>
    /// <returns>
    /// Returns a compiled selector expression or null if no
    /// selector was added.
    /// </returns>
    Expression<Func<T, T>>? TryCompile<T>();
}
