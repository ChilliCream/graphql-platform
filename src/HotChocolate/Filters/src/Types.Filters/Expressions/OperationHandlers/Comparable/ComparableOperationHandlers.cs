using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
        private static Expression GetProperty(
            FilterOperation operation,
            IFilterVisitorContext<Expression> context)
        {
            Expression property = context.GetInstance();

            if (!operation.IsSimpleArrayType())
            {
                property = Expression.Property(context.GetInstance(), operation.Property);
            }
            return property;
        }

        private static object ParseValue(
            object parsedValue,
            FilterOperation operation,
            IInputType type,
            IFilterVisitorContext<Expression> context)
        {
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
