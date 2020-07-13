using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableEqualsOperationHandler
        : ComparableOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            MemberExpression property,
            Func<object> parseValue,
            out Expression expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.Equals:
                    expression = FilterExpressionBuilder.Equals(
                        property, parseValue());
                    return true;

                case FilterOperationKind.NotEquals:
                    expression = FilterExpressionBuilder.NotEquals(
                        property, parseValue());
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
