using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableGreaterThanOrEqualsOperationHandler
        : ComparableOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            Func<object> parseValue,
            out Expression expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.GreaterThanOrEquals:
                    expression = FilterExpressionBuilder.GreaterThanOrEqual(
                        property, parseValue());
                    return true;

                case FilterOperationKind.NotGreaterThanOrEquals:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.GreaterThanOrEqual(
                            property, parseValue())
                     );
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
