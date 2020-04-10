using System;
using System.Diagnostics.CodeAnalysis;
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

        public static Expression EndsWith(
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

                return FilterExpressionBuilder.EndsWith(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotEndsWith(
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

                return FilterExpressionBuilder.Not(
                    FilterExpressionBuilder.EndsWith(property, parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression StartsWith(
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

                return FilterExpressionBuilder.StartsWith(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotStartsWith(
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

                return FilterExpressionBuilder.Not(
                    FilterExpressionBuilder.StartsWith(property, parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression In(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotIn(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = type.ParseLiteral(value);

                return FilterExpressionBuilder.Not(
                    FilterExpressionBuilder.In(
                        property,
                        operation.Property.PropertyType,
                        parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static Expression GetProperty(
            FilterOperation operation,
            IQueryableFilterVisitorContext context)
        {
            Expression property = context.GetInstance();

            if (!operation.IsSimpleArrayType())
            {
                property = Expression.Property(context.GetInstance(), operation.Property);
            }
            return property;
        }
    }
}
