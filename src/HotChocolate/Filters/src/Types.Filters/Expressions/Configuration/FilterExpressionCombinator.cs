using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class FilterExpressionCombinator
    {
        public static Expression CombineWithAnd(
               IReadOnlyList<Expression> operations) =>
            CombineWithCombinator(operations, Expression.AndAlso);

        public static Expression CombineWithOr(
               IReadOnlyList<Expression> operations) =>
            CombineWithCombinator(operations, Expression.OrElse);

        public static Expression CombineWithCombinator(
            IReadOnlyList<Expression> operations,
            Func<Expression, Expression, Expression> combine)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            Expression combined = operations[0];

            for (var i = 1; i < operations.Count; i++)
            {
                combined = combine(combined, operations[i]);
            }

            return combined;
        }
    }
}
