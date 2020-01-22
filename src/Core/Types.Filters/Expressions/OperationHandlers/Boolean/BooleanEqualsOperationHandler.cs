using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class BooleanEqualsOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            IQueryableFilterVisitorContext context,
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            Expression instance,
            out Expression expression)
        {
            if (operation.Type == typeof(bool) && type.IsInstanceOfType(value))
            {
                Expression property = instance;

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(instance, operation.Property);
                }

                object parserValue = type.ParseLiteral(value);

                switch (operation.Kind)
                {
                    case FilterOperationKind.Equals:
                        expression = FilterExpressionBuilder.Equals(
                            property, parserValue);
                        return true;

                    case FilterOperationKind.NotEquals:
                        expression = FilterExpressionBuilder.NotEquals(
                            property, parserValue);
                        return true;

                }
            }

            expression = null;
            return false;
        }
    }
}
