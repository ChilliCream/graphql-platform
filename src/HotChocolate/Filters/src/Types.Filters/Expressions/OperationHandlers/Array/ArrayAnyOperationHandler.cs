using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions;

[Obsolete("Use HotChocolate.Data.")]
public class ArrayAnyOperationHandler
    : IExpressionOperationHandler
{
    public bool TryHandle(
        FilterOperation operation,
        IInputType type,
        IValueNode value,
        IQueryableFilterVisitorContext context,
        [NotNullWhen(true)] out Expression? expression)
    {
        if (operation.Kind == FilterOperationKind.ArrayAny &&
            type.IsInstanceOfType(value) &&
            context.InputParser.ParseLiteral(value, type) is bool parsedValue)
        {
            var property =
                Expression.Property(context.GetInstance(), operation.Property);
            var propertyType = operation.Type;

            if (operation.TryGetSimpleFilterBaseType(out var baseType))
            {
                propertyType = baseType;
            }

            if (parsedValue)
            {
                expression = FilterExpressionBuilder.Any(
                    propertyType,
                    property);
            }
            else
            {
                expression = FilterExpressionBuilder.Not(
                    FilterExpressionBuilder.Any(
                        propertyType,
                        property));
            }
            if (context.InMemory)
            {
                expression =
                    FilterExpressionBuilder.NotNullAndAlso(property, expression);
            }
            return true;
        }
        expression = null;
        return false;
    }
}