using System;
using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableFieldSelector : QueryableSortOperation
    {
        protected QueryableFieldSelector(
            ParameterExpression parameterExpression)
            : base(parameterExpression, parameterExpression)
        {
        }

        protected QueryableFieldSelector(
            Expression selector,
            ParameterExpression parameterExpression)
            : base(selector, parameterExpression)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            throw new InvalidOperationException(DataResources.SortInvocation_Cannot_SortOnFields);
        }

        public override Expression CompileThenBy(Expression expression)
        {
            throw new InvalidOperationException(DataResources.SortInvocation_Cannot_SortOnFields);
        }

        public QueryableFieldSelector WithSelector(Expression selector) =>
            new QueryableFieldSelector(selector, ParameterExpression);

        public static QueryableFieldSelector New(Type initialType) =>
            new QueryableFieldSelector(Expression.Parameter(initialType, "x"));
    }
}
