using System.Linq.Expressions;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Marten.Sorting.Handlers;

public class MartenQueryableDescendingSortOperationHandler : QueryableDescendingSortOperationHandler
{
    protected override QueryableSortOperation HandleOperation(
        QueryableSortContext context,
        QueryableFieldSelector fieldSelector,
        ISortField field,
        ISortEnumValue? sortEnumValue)
    {
        return MartenDescendingSortOperation.From(fieldSelector);
    }

    private class MartenDescendingSortOperation : QueryableSortOperation
    {
        private MartenDescendingSortOperation(QueryableFieldSelector fieldSelector)
            : base(fieldSelector)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            var translatedSelector = MartenExpressionTranslator.TranslateSortExpression(Selector);
            var generatedExpression = Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderByDescending),
                new[] { ParameterExpression.Type, translatedSelector.Type },
                expression,
                Expression.Lambda(translatedSelector, ParameterExpression));
            return generatedExpression;
        }

        public override Expression CompileThenBy(Expression expression)
        {
            var translatedSelector = MartenExpressionTranslator.TranslateSortExpression(Selector);
            var generatedExpression = Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.ThenByDescending),
                new[] { ParameterExpression.Type, translatedSelector.Type },
                expression,
                Expression.Lambda(translatedSelector, ParameterExpression));
            return generatedExpression;
        }

        public static MartenDescendingSortOperation From(QueryableFieldSelector selector) => new(selector);
    }
}
