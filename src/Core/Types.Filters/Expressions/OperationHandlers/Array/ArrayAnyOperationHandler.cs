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
            out Expression expression)
        {
            if (operation.Kind == FilterOperationKind.ArrayAny &&
                type.IsInstanceOfType(value) &&
                type.ParseLiteral(value) is bool parsedValue)
            {
                Expression property = instance;

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(instance, operation.Property);
                }

                if (parsedValue)
                {
                    expression = FilterExpressionBuilder.Any(
                        operation.Type,
                        property);
                }
                else
                {
                    expression = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.Any(
                            operation.Type,
                            property));
                }
                return true;
            }
            expression = null;
            return false;
        }
    }
}
