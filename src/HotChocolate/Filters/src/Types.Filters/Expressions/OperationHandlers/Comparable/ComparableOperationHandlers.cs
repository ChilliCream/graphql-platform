using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
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
