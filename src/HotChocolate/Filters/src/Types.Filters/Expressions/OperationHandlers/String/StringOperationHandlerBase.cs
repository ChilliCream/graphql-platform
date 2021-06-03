using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    [Obsolete("Use HotChocolate.Data.")]
    public abstract class StringOperationHandlerBase
        : IExpressionOperationHandler
    {
        public bool TryHandle(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)] out Expression? expression)
        {
            if (operation.Type == typeof(string) &&
                    type.IsInstanceOfType(value))
            {
                object? parsedValue = type.ParseLiteral(value);

                Expression property = context.GetInstance();

                if (!operation.IsSimpleArrayType())
                {
                    property = Expression.Property(
                        context.GetInstance(), operation.Property);
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
            [NotNullWhen(true)] out Expression? expression);
    }
}
