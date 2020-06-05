using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ArrayOperationHandler
    {
        public static bool ArrayAny(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            FilterOperationField _,
            IFilterVisitorContext<Expression> context,
            [NotNullWhen(true)] out Expression result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null!;
                return false;
            }

            if (FilterOperationKind.ArrayAny.Equals(operation.Kind) &&
                type.IsInstanceOfType(value) &&
                parsedValue is bool parsedBool &&
                context is QueryableFilterVisitorContext queryableContext)
            {
                MemberExpression property =
                    Expression.Property(context.GetInstance(), operation.Property);

                Type propertType = operation.Type;

                if (operation.TryGetElementType(out Type? baseType))
                {
                    propertType = baseType;
                }

                Expression expression;
                if (parsedBool)
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

                if (queryableContext.InMemory)
                {
                    expression =
                        FilterExpressionBuilder.NotNullAndAlso(property, expression);
                }

                result = expression;
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
