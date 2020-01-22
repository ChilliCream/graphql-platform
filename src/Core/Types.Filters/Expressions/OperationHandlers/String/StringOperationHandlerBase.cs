using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public abstract class StringOperationHandlerBase
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
            if (operation.Type == typeof(string)
                && type.IsInstanceOfType(value))
            {
                object parsedValue = type.ParseLiteral(value);

                Expression property = instance;

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(instance, operation.Property);
                }


                return TryCreateExpression(
                    operation,
                    property,
                    parsedValue,
                    out expression);
            }

            expression = null;
            return false;
        }

        protected abstract bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            object parsedValue,
            out Expression expression);
    }
}
