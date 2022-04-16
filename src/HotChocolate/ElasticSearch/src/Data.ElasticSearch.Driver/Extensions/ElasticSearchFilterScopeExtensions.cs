using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Provides extension for <see cref="ElasticSearchFilterScope"/>
/// </summary>
public static class ElasticSearchFilterScopeExtensions
{
    /// <summary>
    /// Returns the currently selected path of this scope
    /// </summary>
    public static string GetPath(this ElasticSearchFilterScope scope) =>
        string.Join(".", scope.Path.Reverse());

    /// <summary>
    /// Builds a <see cref="QueryDefinition"/> from the state aggregated in the scope
    /// </summary>
    public static bool TryCreateQuery(
        this ElasticSearchFilterScope scope,
        [NotNullWhen(true)] out QueryDefinition? query)
    {
        query = null;

        if (scope.Level.Peek().Count == 0)
        {
            return false;
        }

        if (scope.Level.Peek().Peek() is not BoolOperation operation)
        {
            return false;
        }

        ISearchOperation[] queries = Array.Empty<ISearchOperation>();
        ISearchOperation[] filters = Array.Empty<ISearchOperation>();
        if (KindOperationRewriter.Query.Rewrite(operation) is BoolOperation rewrittenQuery &&
            rewrittenQuery is {Must.Count: > 0} or {Should.Count: > 0})
        {
            queries = new ISearchOperation[] {rewrittenQuery};
        }

        if (KindOperationRewriter.Filter.Rewrite(operation) is BoolOperation rewrittenFilter &&
            rewrittenFilter is {Must.Count: > 0} or {Should.Count: > 0})
        {
            filters = new ISearchOperation[] {rewrittenFilter};
        }

        query = new QueryDefinition(queries, filters);
        return true;
    }
}
