using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableAscendingSortOperationHandler : QueryableOperationHandlerBase
    {
        public QueryableAscendingSortOperationHandler() : base(DefaultSortOperations.Ascending)
        {
        }

        protected override QueryableSortOperation HandleOperation(
            QueryableSortContext context,
            QueryableFieldSelector fieldSelector,
            ISortField field,
            ISortEnumValue? sortEnumValue)
        {
            return AscendingSortOperation.From(fieldSelector);
        }

        private class AscendingSortOperation : QueryableSortOperation
        {
            protected AscendingSortOperation(QueryableFieldSelector fieldSelector)
                : base(fieldSelector)
            {
            }

            public override Expression CompileOrderBy(Expression expression)
            {
                return Expression.Call(
                    expression.GetEnumerableKind(),
                    nameof(Queryable.OrderBy),
                    new[] {ParameterExpression.Type, Selector.Type},
                    expression,
                    Selector);
            }

            public override Expression CompileThenBy(Expression expression)
            {
                return Expression.Call(
                    expression.GetEnumerableKind(),
                    nameof(Queryable.ThenBy),
                    new[] {ParameterExpression.Type, Selector.Type},
                    expression,
                    Selector);
            }

            public static AscendingSortOperation From(QueryableFieldSelector selector) =>
                new AscendingSortOperation(selector);
        }
    }
}
