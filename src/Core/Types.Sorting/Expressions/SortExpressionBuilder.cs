using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace HotChocolate.Types.Sorting
{
    public static class SortExpressionBuilder
    {
        private static readonly ConstantExpression _null =
            Expression.Constant(null, typeof(object));

        public static Expression IsNull(Expression expression)
        {
            return Expression.Equal(expression, _null);
        }

        public static Expression IfNullThenDefault(
            Expression left,
            Expression right,
            DefaultExpression defaultExpression)
        {
            return Expression.Condition(IsNull(left), defaultExpression, right);
        }
    }
}
