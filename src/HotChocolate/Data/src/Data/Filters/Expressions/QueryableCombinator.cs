using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableCombinator
        : FilterOperationCombinator<Expression, QueryableFilterContext>
    {
        public override bool TryCombineOperations(
            QueryableFilterContext context,
            Queue<Expression> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out Expression combined)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            combined = operations.Dequeue();

            while (operations.Count > 0)
            {
                combined = combinator switch
                {
                    FilterCombinator.AND => Expression.AndAlso(combined, operations.Dequeue()),
                    FilterCombinator.OR => Expression.OrElse(combined, operations.Dequeue()),
                    _ => throw new InvalidOperationException(),
                };
            }

            return true;
        }
    }
}
