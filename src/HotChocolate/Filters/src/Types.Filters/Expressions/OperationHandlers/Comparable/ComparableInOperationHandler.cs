using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
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

                switch (operation.Kind)
                {
                    case FilterOperationKind.In:
                        expression = FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            ParseValue());
                        return true;

                    case FilterOperationKind.NotIn:
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
                var parsedValue = type.ParseLiteral(value);
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
        }
    }
}
