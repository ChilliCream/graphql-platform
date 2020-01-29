using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class StringContainsOperationHandler
        : StringOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            object parsedValue, out
            Expression expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.Contains:
                    expression = FilterExpressionBuilder.Contains(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotContains:
                    expression = FilterExpressionBuilder.NotContains(
                        property, parsedValue);
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
