using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions;

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

    private sealed class AscendingSortOperation : QueryableSortOperation
    {
        private AscendingSortOperation(QueryableFieldSelector fieldSelector)
            : base(fieldSelector)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderBy),
                [ParameterExpression.Type, Selector.Type,],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public override Expression CompileThenBy(Expression expression)
        {
            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.ThenBy),
                [ParameterExpression.Type, Selector.Type,],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public static AscendingSortOperation From(QueryableFieldSelector selector) =>
            new AscendingSortOperation(selector);
    }
}
