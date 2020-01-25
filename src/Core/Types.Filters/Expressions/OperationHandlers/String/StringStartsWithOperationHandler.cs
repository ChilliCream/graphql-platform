using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class StringStartsWithOperationHandler
        : StringOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            object parsedValue,
            out Expression expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.StartsWith:
                    expression = FilterExpressionBuilder.StartsWith(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotStartsWith:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.StartsWith(
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
