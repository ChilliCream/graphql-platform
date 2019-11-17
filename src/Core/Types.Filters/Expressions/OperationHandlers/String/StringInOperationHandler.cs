using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public class StringInOperationHandler
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
            if (operation.Type == typeof(string) && type.IsInstanceOfType(value))
            {
                Expression property = instance;

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(instance, operation.Property);
                }

                var parsedValue = type.ParseLiteral(value);

                switch (operation.Kind)
                {
                    case FilterOperationKind.In:
                        expression = FilterExpressionBuilder.In(
                            property,
                            operation.Property.PropertyType,
                            parsedValue);
                        return true;

                    case FilterOperationKind.NotIn:
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
