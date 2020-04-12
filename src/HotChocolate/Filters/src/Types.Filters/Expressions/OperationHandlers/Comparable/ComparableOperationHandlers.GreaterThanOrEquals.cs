using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
        public static Expression GreaterThanOrEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = ParseValue(operation, type, value, context);

                return FilterExpressionBuilder.GreaterThanOrEqual(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotGreaterThanOrEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = ParseValue(operation, type, value, context);

                return FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.GreaterThanOrEqual(property, parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
