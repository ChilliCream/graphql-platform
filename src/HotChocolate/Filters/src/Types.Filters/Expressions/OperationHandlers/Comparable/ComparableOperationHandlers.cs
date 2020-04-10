using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
        public static Expression Equals(
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
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                var parsedValue = ParseValue(operation, type, value, context);

                return FilterExpressionBuilder.NotEquals(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression GreaterThan(
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

                return FilterExpressionBuilder.GreaterThan(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotGreaterThan(
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
                        FilterExpressionBuilder.GreaterThan(property, parsedValue));
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

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

        public static Expression LowerThan(
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
            if (operation.Type == typeof(IComparable)
               && type.IsInstanceOfType(value))
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

        public static Expression LowerThanOrEquals(
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

                return FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static Expression NotLowerThanOrEquals(
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
                        FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue));
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
                var parsedValue = ParseValue(operation, type, value, context);

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
                var parsedValue = ParseValue(operation, type, value, context);

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

        private static object ParseValue(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            var parsedValue = type.ParseLiteral(value);
            if (type.IsListType())
            {
                Type elementType = type.ElementType().ToClrType();

                if (operation.Property.PropertyType != elementType)
                {
                    Type listType = typeof(List<>).MakeGenericType(
                        operation.Property.PropertyType);

                    parsedValue = context.TypeConverter.Convert(
                        typeof(object),
                        listType,
                        parsedValue);
                }

                return parsedValue;
            }
            else
            {
                if (!operation.Property.PropertyType.IsInstanceOfType(parsedValue))
                {
                    parsedValue = context.TypeConverter.Convert(
                        typeof(object),
                        operation.Property.PropertyType,
                        parsedValue);
                }

                return parsedValue;
            }
        }
    }
}
