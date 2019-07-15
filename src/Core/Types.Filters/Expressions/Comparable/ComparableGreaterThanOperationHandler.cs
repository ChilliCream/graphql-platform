using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableGreaterThanOperationHandler
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
                case FilterOperationKind.GreaterThan:
                    expression = FilterExpressionBuilder.GreaterThan(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotGreaterThan:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.GreaterThan(
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
