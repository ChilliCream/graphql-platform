using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class StringOperationHandlers
    {
        public static Expression Contains(
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

                return FilterExpressionBuilder.Contains(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotContains(
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

                return FilterExpressionBuilder.NotContains(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
