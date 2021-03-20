using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
    public sealed class StringEqualsOperationHandler
        : StringOperationHandlerBase
    {
        protected override bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            object parsedValue,
            [NotNullWhen(true)] out Expression? expression)
        {
            switch (operation.Kind)
            {
                case FilterOperationKind.Equals:
                    expression = FilterExpressionBuilder.Equals(
                        property, parsedValue);
                    return true;

                case FilterOperationKind.NotEquals:
                    expression = FilterExpressionBuilder.NotEquals(
                        property, parsedValue);
                    return true;

                default:
                    expression = null;
                    return false;
            }
        }
    }
}
