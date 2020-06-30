using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class StringOperationHandlers
    {
        public static bool Contains(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)]out Expression? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);

                result = FilterExpressionBuilder.Contains(property, parsedValue);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotContains(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IQueryableFilterVisitorContext context,
            [NotNullWhen(true)]out Expression? result)
        {
            object parsedValue = type.ParseLiteral(value);

            if (parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(operation, type, value, context));

                result = null;
                return false;
            }

            if (operation.Type == typeof(string) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);

                result = FilterExpressionBuilder.NotContains(property, parsedValue);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
