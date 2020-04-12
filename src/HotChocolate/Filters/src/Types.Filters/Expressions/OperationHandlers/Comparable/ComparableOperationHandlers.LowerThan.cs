using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
        public static Expression LowerThan(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = ParseValue(operation, type, value, context);

                return FilterExpressionBuilder.LowerThan(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotLowerThan(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = ParseValue(operation, type, value, context);

                return FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThan(property, parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
