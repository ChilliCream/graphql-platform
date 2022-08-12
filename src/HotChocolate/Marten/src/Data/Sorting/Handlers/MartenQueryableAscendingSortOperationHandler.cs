using System.Linq.Expressions;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Marten.Sorting.Handlers;

public class MartenQueryableAscendingSortOperationHandler : QueryableAscendingSortOperationHandler
{
    protected override QueryableSortOperation HandleOperation(
        QueryableSortContext context,
        QueryableFieldSelector fieldSelector,
        ISortField field,
        ISortEnumValue? sortEnumValue)
    {
        return MartenAscendingSortOperation.From(fieldSelector);
    }

    private class MartenAscendingSortOperation : QueryableSortOperation
    {
        private MartenAscendingSortOperation(QueryableFieldSelector fieldSelector)
            : base(fieldSelector)
        {
        }

        public override Expression CompileOrderBy(Expression expression)
        {
            var translatedSelector = MartenExpressionTranslator.TranslateSortExpression(Selector);
            var generatedExpression = Expression.Call(
                expression.GetEnumerableKind(),
                nameof(Queryable.OrderBy),
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
                nameof(Queryable.ThenBy),
                new[] { ParameterExpression.Type, translatedSelector.Type },
                expression,
                Expression.Lambda(translatedSelector, ParameterExpression));
            return generatedExpression;
        }

        public static MartenAscendingSortOperation From(QueryableFieldSelector selector) => new(selector);
    }
}
