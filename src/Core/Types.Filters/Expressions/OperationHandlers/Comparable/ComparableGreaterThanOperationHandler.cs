using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableGreaterThanOperationHandler
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
                case FilterOperationKind.GreaterThan:
                    expression = FilterExpressionBuilder.GreaterThan(
                        property, parseValue());
                    return true;

                case FilterOperationKind.NotGreaterThan:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.GreaterThan(
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
