using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class ArrayAnyOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            ITypeConversion converter,
            bool inMemory,
            out Expression expression)
        {
            if (operation.Kind == FilterOperationKind.ArrayAny &&
                type.IsInstanceOfType(value) &&
                type.ParseLiteral(value) is bool parsedValue)
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);
                Type propertType;

                if (operation.TryGetSimpleFilterBaseType(out Type baseType))
                {
                    propertType = baseType;
                }
                else
                {
                    propertType = operation.Type;
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
                if (inMemory)
                {
                    expression = FilterExpressionBuilder.NotNullAndAlso(property, expression);
                }
                return true;
            }
            expression = null;
            return false;
        }
    }
}
