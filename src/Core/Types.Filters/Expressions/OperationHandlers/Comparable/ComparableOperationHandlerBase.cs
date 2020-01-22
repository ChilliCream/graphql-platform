using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public abstract class ComparableOperationHandlerBase
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
            if (operation.Type == typeof(IComparable)
                && type.IsInstanceOfType(value))
            {

                Expression property = instance;

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(instance, operation.Property);
                }


                return TryCreateExpression(
                    operation,
                    property,
                    ParseValue,
                    out expression);
            }

            expression = null;
            return false;

            object ParseValue()
            {

                var parsedValue = type.ParseLiteral(value);

                if (!operation.Property.PropertyType.IsInstanceOfType(parsedValue))
                {
                    parsedValue = context.TypeConverter.Convert(
                        typeof(object),
                        operation.Property.PropertyType,
                        parsedValue);
                }

                return parsedValue;
            }
        }

        protected abstract bool TryCreateExpression(
            FilterOperation operation,
            Expression property,
            Func<object> parseValue,
            out Expression expression);
    }
}
