using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public abstract class ComparableOperationHandlerBase
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
            if (operation.Type == typeof(IComparable)
                && type.IsInstanceOfType(value))
            {
                MemberExpression property =
                    Expression.Property(instance, operation.Property);
                var parsedValue = type.ParseLiteral(value);

                if (operation.Property.PropertyType
                    .IsInstanceOfType(parsedValue))
                {
                    parsedValue = converter.Convert(
                        typeof(object),
                        operation.Property.PropertyType,
                        parsedValue);
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
            MemberExpression property,
            object parsedValue,
            out Expression expression);
    }
}
