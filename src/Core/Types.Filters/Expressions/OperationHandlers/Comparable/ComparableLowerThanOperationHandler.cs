using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableLowerThanOperationHandler
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
                case FilterOperationKind.LowerThan:
                    expression = FilterExpressionBuilder.LowerThan(
                        property, parseValue());
                    return true;

                case FilterOperationKind.NotLowerThan:
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThan(
                            property, parseValue()));
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
