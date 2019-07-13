using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ComparableLowerThanOrEqualsOperationHandler
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
                case FilterOperationKind.LowerThanOrEquals:
                    expression = FilterExpressionBuilder.LowerThanOrEqual(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotLowerThanOrEquals:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThanOrEqual(
                            property, parsedValue));
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
