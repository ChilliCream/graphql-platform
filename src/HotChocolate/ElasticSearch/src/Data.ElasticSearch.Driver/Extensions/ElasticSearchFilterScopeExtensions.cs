using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static HotChocolate.Data.ElasticSearch.BoolOperation;
using static HotChocolate.Data.ElasticSearch.Filters.KindOperationRewriter;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Provides extension for <see cref="ElasticSearchFilterScope"/>
/// </summary>
public static class ElasticSearchFilterScopeExtensions
{
    /// <summary>
    /// Builds a <see cref="BoolOperation"/> from the state aggregated in the scope
    /// </summary>
    public static bool TryCreateQuery(
        this ElasticSearchFilterScope scope,
        [NotNullWhen(true)] out BoolOperation? query)
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

        query = (Query.Rewrite(operation), Filter.Rewrite(operation)) switch
        {
            (BoolOperation q, null) => q,
            ({ } q, { } f) => Create(must: new[] { q }, filter: new[] { f }),
            ({ } q, null) => Create(must: new[] { q }, filter: Array.Empty<ISearchOperation>()),
            (null, { } f) => Create(must: Array.Empty<ISearchOperation>(), filter: new[] { f }),
            _ => null
        };

        return query is not null;
    }
}
