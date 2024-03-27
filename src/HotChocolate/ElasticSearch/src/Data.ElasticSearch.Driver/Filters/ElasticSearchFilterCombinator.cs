using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <inheritdoc />
public class ElasticSearchFilterCombinator
    : FilterOperationCombinator<ElasticSearchFilterVisitorContext, ISearchOperation>
{
    /// <inheritdoc />
    public override bool TryCombineOperations(
        ElasticSearchFilterVisitorContext context,
        Queue<ISearchOperation> operations,
        FilterCombinator combinator,
        [NotNullWhen(true)] out ISearchOperation combined)
    {
        if (operations.Count == 0)
        {
            throw ThrowHelper.Filtering_ElasticSearchCombinator_QueueEmpty(this);
        }

        combined = combinator switch
        {
            FilterCombinator.And => CombineWithAnd(operations),
            FilterCombinator.Or => CombineWithOr(operations),
            _ => throw ThrowHelper
                .Filtering_ElasticSearchCombinator_InvalidCombinator(this, combinator)
        };

        return true;
    }

    private static ISearchOperation CombineWithAnd(Queue<ISearchOperation> operations)
    {
        if (operations.Count == 0)
        {
            throw new InvalidOperationException();
        }

        return BoolOperation.Create(must: operations.ToArray());
    }

    private static ISearchOperation CombineWithOr(Queue<ISearchOperation> operations)
    {
        if (operations.Count == 0)
        {
            throw new InvalidOperationException();
        }

        return BoolOperation.Create(should: operations.ToArray());
    }
}
