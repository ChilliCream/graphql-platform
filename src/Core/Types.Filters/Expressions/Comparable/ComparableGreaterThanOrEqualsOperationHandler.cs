using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableGreaterThanOrEqualsOperationHandler
        : ComparableOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            MemberExpression property,
            object parsedValue,
            out Expression expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.GreaterThanOrEquals:
                    expression = FilterExpressionBuilder.GreaterThanOrEqual(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotGreaterThanOrEquals:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.GreaterThanOrEqual(
                            property, parsedValue)
                     );
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
