using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters.Expressions;

public static class QueryableFilterVisitorContextExtensions
{
    /// <summary>
    /// Reads the current closure from the context
    /// </summary>
    /// <param name="context">The context</param>
    /// <returns>The current closure</returns>
    public static QueryableScope GetClosure(
        this QueryableFilterContext context) =>
        (QueryableScope)context.GetScope();

    /// <summary>
    /// Tries to build the an expression based on the items that are stored on the scope
    /// </summary>
    /// <param name="context">the context</param>
    /// <param name="expression">The query that was build</param>
    /// <returns>True in case the query has been build successfully, otherwise false</returns>
    public static bool TryCreateLambda(
        this QueryableFilterContext context,
        [NotNullWhen(true)] out LambdaExpression? expression)
    {
        if (context.Scopes.TryPeekElement(out var scope) &&
            scope is QueryableScope closure &&
            closure.Level.TryPeekElement(out var levels) &&
            levels.TryPeekElement(out var level))
        {
            expression = Expression.Lambda(level, closure.Parameter);
            return true;
        }

        expression = null;
        return false;
    }

    /// <summary>
    /// Tries to build the a typed expression based on the items that are stored on the scope
    /// </summary>
    /// <param name="context">the context</param>
    /// <param name="expression">The query that was build</param>
    /// <typeparam name="T">The generic type of the expression</typeparam>
    /// <returns>True in case the query has been build successfully, otherwise false</returns>
    public static bool TryCreateLambda<T>(
        this QueryableFilterContext context,
        [NotNullWhen(true)] out Expression<T>? expression)
    {
        if (context.Scopes.TryPeekElement(out var scope) &&
            scope is QueryableScope closure &&
            closure.Level.TryPeekElement(out var levels) &&
            levels.TryPeekElement(out var level))
        {
            expression = Expression.Lambda<T>(level, closure.Parameter);
            return true;
        }

        expression = null;
        return false;
    }
}
