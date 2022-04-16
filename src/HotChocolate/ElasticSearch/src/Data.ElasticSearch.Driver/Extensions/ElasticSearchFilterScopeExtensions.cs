using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HotChocolate.Data.ElasticSearch.Filters;

public static class ElasticSearchFilterScopeExtensions
{
    public static string GetPath(this ElasticSearchFilterScope scope) =>
        string.Join(".", scope.Path.Reverse());

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
