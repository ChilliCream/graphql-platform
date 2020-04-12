using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ArrayOperationHandler
    {
        public static Expression ArrayAny(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context)
        {
            if (operation.Kind == FilterOperationKind.ArrayAny &&
                type.IsInstanceOfType(value) &&
                type.ParseLiteral(value) is bool parsedValue)
            {
                MemberExpression property =
                    Expression.Property(context.GetInstance(), operation.Property);
                Type propertType = operation.Type;

                if (operation.TryGetSimpleFilterBaseType(out Type? baseType))
                {
                    propertType = baseType;
                }

                Expression expression;
                if (parsedValue)
                {
                    expression = FilterExpressionBuilder.Any(
                        propertType,
                        property);
                }
                else
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.Any(
                            propertType,
                            property));
                }

                if (context.InMemory)
                {
                    expression =
                        FilterExpressionBuilder.NotNullAndAlso(property, expression);
                }

                return expression;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
