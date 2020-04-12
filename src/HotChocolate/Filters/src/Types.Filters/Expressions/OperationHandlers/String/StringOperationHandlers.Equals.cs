using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class StringOperationHandlers
    {
        public static Expression Equals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.Equals(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.NotEquals(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
