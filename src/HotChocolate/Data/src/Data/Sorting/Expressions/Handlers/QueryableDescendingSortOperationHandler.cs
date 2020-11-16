using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableDescendingSortOperationHandler : QueryableOperationHandlerBase
    {
        public QueryableDescendingSortOperationHandler() : base(DefaultSortOperations.Descending)
        {
        }

        protected override QueryableSortOperation HandleOperation(
            QueryableSortContext context,
            QueryableFieldSelector fieldSelector,
            ISortField field,
            ISortEnumValue? sortEnumValue)
        {
            return DescendingSortOperation.From(fieldSelector);
        }

        private class DescendingSortOperation : QueryableSortOperation
        {
            protected DescendingSortOperation(QueryableFieldSelector fieldSelector)
                : base(fieldSelector)
            {
            }

            public override Expression CompileOrderBy(Expression expression)
            {
                return Expression.Call(
                    expression.GetEnumerableKind(),
                    nameof(Queryable.OrderByDescending),
                    new[] {ParameterExpression.Type, Selector.Type},
                    expression,
                    Expression.Lambda(Selector, ParameterExpression));
            }

            public override Expression CompileThenBy(Expression expression)
            {
                return Expression.Call(
                    expression.GetEnumerableKind(),
                    nameof(Queryable.ThenByDescending),
                    new[] {ParameterExpression.Type, Selector.Type},
                    expression,
                    Expression.Lambda(Selector, ParameterExpression));
            }

            public static DescendingSortOperation From(QueryableFieldSelector selector) =>
                new DescendingSortOperation(selector);
        }
    }
}
