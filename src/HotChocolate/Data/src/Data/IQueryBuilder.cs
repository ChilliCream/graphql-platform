using HotChocolate.Resolvers;

namespace HotChocolate.Data;

/// <summary>
/// An abstraction that is used to translate GraphQL queries to other query languages.
/// </summary>
public interface IQueryBuilder
{
    /// <summary>
    /// Prepares the execution state to build a query or a query part.
    /// </summary>
    /// <param name="context">
    /// The field execution context.
    /// </param>
    void Prepare(IMiddlewareContext context);

    /// <summary>
    /// Applies the query or query part to the execution context without executing it.
    /// </summary>
    /// <param name="context">
    /// The field execution context.
    /// </param>
    void Apply(IMiddlewareContext context);
}
