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
        SortEnumValue? sortEnumValue)
    {
        return AscendingSortOperation.From(fieldSelector);
    }

    public static QueryableAscendingSortOperationHandler Create(SortProviderContext context) => new();

    private sealed class AscendingSortOperation : QueryableSortOperation
    {
        private AscendingSortOperation(QueryableFieldSelector fieldSelector)
            : base(fieldSelector)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            if (QueryableSortExpressionOptimizer.TryRewriteSelectorToSource(
                expression,
                ParameterExpression,
                Selector,
                out var rewrittenSource,
                out var rewrittenSelector,
                out var projection))
            {
                var sortedSource = Expression.Call(
                    rewrittenSource.GetEnumerableKind(),
                    nameof(Queryable.OrderBy),
                    [rewrittenSelector.Parameters[0].Type, rewrittenSelector.ReturnType],
                    rewrittenSource,
                    rewrittenSelector);

                return QueryableSortExpressionOptimizer.ReapplyProjection(
                    sortedSource,
                    projection);
            }

            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderBy),
                [ParameterExpression.Type, Selector.Type],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public override Expression CompileThenBy(Expression expression)
        {
            if (QueryableSortExpressionOptimizer.TryRewriteSelectorToSource(
                expression,
                ParameterExpression,
                Selector,
                out var rewrittenSource,
                out var rewrittenSelector,
                out var projection))
            {
                var sortedSource = Expression.Call(
                    rewrittenSource.GetEnumerableKind(),
                    nameof(Queryable.ThenBy),
                    [rewrittenSelector.Parameters[0].Type, rewrittenSelector.ReturnType],
                    rewrittenSource,
                    rewrittenSelector);

                return QueryableSortExpressionOptimizer.ReapplyProjection(
                    sortedSource,
                    projection);
            }

            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.ThenBy),
                [ParameterExpression.Type, Selector.Type],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public static AscendingSortOperation From(QueryableFieldSelector selector) =>
            new AscendingSortOperation(selector);
    }
}
