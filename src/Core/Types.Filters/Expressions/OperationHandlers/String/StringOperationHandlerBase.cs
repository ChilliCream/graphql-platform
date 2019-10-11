using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Filters.Expressions
{
    public abstract class StringOperationHandlerBase
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
