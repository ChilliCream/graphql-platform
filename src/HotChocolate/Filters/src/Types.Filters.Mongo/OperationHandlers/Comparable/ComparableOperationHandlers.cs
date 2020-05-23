using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static partial class ComparableOperationHandlers
    {
        private static object ParseValue(
            object parsedValue,
            FilterOperation operation,
            IInputType type,
            IFilterVisitorContext<IMongoQuery> context)
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
