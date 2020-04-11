using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
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
                type.ParseLiteral(value) is bool parsedValue)
            {
                MemberExpression property =
                    Expression.Property(context.GetInstance(), operation.Property);
                Type propertType = operation.Type;

                if (operation.TryGetSimpleFilterBaseType(out Type? baseType))
                {
                    propertType = baseType;
                }

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
                return true;
            }
            expression = null;
            return false;
        }
    }
}
