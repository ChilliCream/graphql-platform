using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public sealed class ComparableEqualsOperationHandler
        : ComparableOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            Func<object> parseValue,
            [NotNullWhen(true)] out Expression? expression)
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
