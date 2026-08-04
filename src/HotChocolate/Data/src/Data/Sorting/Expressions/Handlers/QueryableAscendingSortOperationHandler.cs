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
        => AscendingSortOperation.From(fieldSelector);

    public static QueryableAscendingSortOperationHandler Create(SortProviderContext _) => new();

    private sealed class AscendingSortOperation : QueryableSortOperation
    {
        private AscendingSortOperation(QueryableFieldSelector fieldSelector)
            : base(fieldSelector)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            // We try to push the sort through any .Select() projection so the database can sort
            // before projecting. If that works, we apply the sort on the source and re-attach the projection.
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

            // If the optimization is not possible, we fall back to a plain OrderBy on
            // the expression as-is.
            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderBy),
                [ParameterExpression.Type, Selector.Type],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public override Expression CompileThenBy(Expression expression)
        {
            // We try to push the sort through any .Select() projection so the database can sort
            // before projecting. If that works, we apply the sort on the source and re-attach the projection.
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

            // If the optimization is not possible, we fall back to a plain ThenBy on
            // the expression as-is.
            return Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.ThenBy),
                [ParameterExpression.Type, Selector.Type],
                expression,
                Expression.Lambda(Selector, ParameterExpression));
        }

        public static AscendingSortOperation From(QueryableFieldSelector selector)
            => new AscendingSortOperation(selector);
    }
}
