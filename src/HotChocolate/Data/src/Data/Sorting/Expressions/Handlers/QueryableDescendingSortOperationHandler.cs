using System.Linq.Expressions;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableDescendingSortOperationHandler : QueryableOperationHandlerBase
{
    public QueryableDescendingSortOperationHandler() : base(DefaultSortOperations.Descending)
    {
    }

    protected override QueryableSortOperation HandleOperation(
        QueryableSortContext context,
        QueryableFieldSelector fieldSelector,
        ISortField field,
        SortEnumValue? sortEnumValue)
    {
        return DescendingSortOperation.From(fieldSelector);
    }

    public static QueryableDescendingSortOperationHandler Create(SortProviderContext context) => new();

    private sealed class DescendingSortOperation : QueryableSortOperation
    {
        private DescendingSortOperation(QueryableFieldSelector fieldSelector)
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
                    nameof(Queryable.OrderByDescending),
                    [rewrittenSelector.Parameters[0].Type, rewrittenSelector.ReturnType],
                    rewrittenSource,
                    rewrittenSelector);

                return QueryableSortExpressionOptimizer.ReapplyProjection(
                    sortedSource,
                    projection);
            }

            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderByDescending),
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
                    nameof(Queryable.ThenByDescending),
                    [rewrittenSelector.Parameters[0].Type, rewrittenSelector.ReturnType],
                    rewrittenSource,
                    rewrittenSelector);

                return QueryableSortExpressionOptimizer.ReapplyProjection(
                    sortedSource,
                    projection);
            }

            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.ThenByDescending),
                [ParameterExpression.Type, Selector.Type],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public static DescendingSortOperation From(QueryableFieldSelector selector) =>
            new DescendingSortOperation(selector);
    }
}
