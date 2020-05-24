using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class FilterExpressionCombinator
    {
        public static Expression CombineWithAnd(
            Queue<Expression> operations,
            IFilterVisitorContext<Expression> _) =>
            CombineWithCombinator(operations, Expression.AndAlso);

        public static Expression CombineWithOr(
            Queue<Expression> operations,
            IFilterVisitorContext<Expression> _) =>
            CombineWithCombinator(operations, Expression.OrElse);

        public static Expression CombineWithCombinator(
            Queue<Expression> operations,
            Func<Expression, Expression, Expression> combine)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            Expression combined = operations.Dequeue();

            while (operations.Count > 0)
            {
                combined = combine(combined, operations.Dequeue());
            }

            return combined;
        }
    }
}
