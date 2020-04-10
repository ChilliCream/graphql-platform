using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class BooleanOperationHandlers
    {
        public static Expression Equals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(bool) && type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(context.GetInstance(), operation.Property);
                }

                object parserValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.Equals(property, parserValue);
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
            if (operation.Type == typeof(bool) && type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(context.GetInstance(), operation.Property);
                }

                object parserValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.NotEquals(property, parserValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
