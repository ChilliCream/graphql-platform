using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableLowerThanOrEqualsOperationHandler
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
                case FilterOperationKind.LowerThanOrEquals:
                    expression = FilterExpressionBuilder.LowerThanOrEqual(
                        property, parseValue());
                    return true;

                case FilterOperationKind.NotLowerThanOrEquals:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThanOrEqual(
                            property, parseValue()));
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
