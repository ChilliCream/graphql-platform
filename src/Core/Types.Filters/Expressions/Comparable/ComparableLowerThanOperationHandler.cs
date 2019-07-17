using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableLowerThanOperationHandler
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
                case FilterOperationKind.LowerThan:
                    expression = FilterExpressionBuilder.LowerThan(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotLowerThan:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThan(
                            property, parsedValue));
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
