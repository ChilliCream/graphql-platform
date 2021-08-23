using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
    public sealed class ComparableInOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)] out Expression? expression)
        {
            if (operation.Type == typeof(IComparable)
                && type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(context.GetInstance(), operation.Property);
                }

                if (operation.Kind == FilterOperationKind.In)
                {
                    expression = FilterExpressionBuilder.In(
                        property,
                        operation.Property.PropertyType,
                        ParseValue());
                    return true;
                }

                if (operation.Kind == FilterOperationKind.NotIn)
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            ParseValue()));
                    return true;
                }
            }

            expression = null;
            return false;

            object ParseValue()
            {
                object? parsedValue = type.ParseLiteral(value);
                Type elementType = type.ElementType().ToRuntimeType();

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
        }
    }
}
