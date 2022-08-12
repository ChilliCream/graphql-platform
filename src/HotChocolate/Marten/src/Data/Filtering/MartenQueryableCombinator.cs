using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableCombinator
    : FilterOperationCombinator<QueryableFilterContext, Expression>
{
    public override bool TryCombineOperations(
        QueryableFilterContext context,
        Queue<Expression> operations,
        FilterCombinator combinator,
        [NotNullWhen(true)] out Expression? expression)
    {
        if (operations.Count == 0)
        {
            expression = default;
            return false;
        }

        var operation = operations.Dequeue();
        var translatedOperation = MartenExpressionTranslator.TranslateFilterExpression(operation);
        expression = translatedOperation;

        while (operations.Count > 0)
        {
            switch (combinator)
            {
                case FilterCombinator.And:
                {
                    var rightOperand = operations.Dequeue();
                    var translatedRightOperand = MartenExpressionTranslator.TranslateFilterExpression(rightOperand);
                    expression = Expression.AndAlso(expression, translatedRightOperand);
                    break;
                }
                case FilterCombinator.Or:
                {
                    var rightOperand = operations.Dequeue();
                    var translatedRightOperand = MartenExpressionTranslator.TranslateFilterExpression(rightOperand);
                    expression = Expression.OrElse(expression, translatedRightOperand);
                    break;
                }
                default:
                    throw ThrowHelper
                        .Filtering_MartenQueryableCombinator_InvalidCombinator(this, combinator);
            }
        }
        return true;
    }
}
