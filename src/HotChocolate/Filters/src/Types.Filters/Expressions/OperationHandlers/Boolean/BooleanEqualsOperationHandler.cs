using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public class BooleanEqualsOperationHandler
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)] out Expression? expression)
        {
            if (operation.Type == typeof(bool) && type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(context.GetInstance(), operation.Property);
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
