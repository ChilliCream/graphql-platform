using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableCombinator
    : FilterOperationCombinator<QueryableFilterContext, Expression>
{
    public override bool TryCombineOperations(
        QueryableFilterContext context,
        Queue<Expression> operations,
        FilterCombinator combinator,
        [NotNullWhen(true)] out Expression combined)
    {
        if (operations.Count == 0)
        {
            throw ThrowHelper.Filtering_QueryableCombinator_QueueEmpty(this);
        }

        combined = operations.Dequeue();

        while (operations.Count > 0)
        {
            combined = combinator switch
            {
                FilterCombinator.And => Expression.AndAlso(combined, operations.Dequeue()),
                FilterCombinator.Or => Expression.OrElse(combined, operations.Dequeue()),
                _ => throw ThrowHelper
                    .Filtering_QueryableCombinator_InvalidCombinator(this, combinator)
            };
        }

        return true;
    }
}
