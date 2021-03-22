using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
    public class StringInOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)] out Expression? expression)
        {
            if (operation.Type == typeof(string) && type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(
                        context.GetInstance(), operation.Property);
                }

                object? parsedValue = type.ParseLiteral(value);

                if (operation.Kind == FilterOperationKind.In)
                {
                    expression = FilterExpressionBuilder.In(
                        property,
                        operation.Property.PropertyType,
                        parsedValue);
                    return true;
                }

                if (operation.Kind == FilterOperationKind.NotIn)
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            parsedValue));
                    return true;
                }
            }

            expression = null;
            return false;
        }
    }
}
