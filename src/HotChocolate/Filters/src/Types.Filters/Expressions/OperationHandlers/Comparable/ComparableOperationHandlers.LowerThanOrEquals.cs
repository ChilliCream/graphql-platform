using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static partial class ComparableOperationHandlers
    {
        public static bool LowerThanOrEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<Expression> context,
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

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                parsedValue = ParseValue(parsedValue, operation, type, context);

                result = FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool NotLowerThanOrEquals(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<Expression> context,
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

            if (operation.Type == typeof(IComparable) &&
                type.IsInstanceOfType(value))
            {
                Expression property = GetProperty(operation, context);
                parsedValue = ParseValue(parsedValue, operation, type, context);

                result = FilterExpressionBuilder.Not(
                        FilterExpressionBuilder.LowerThanOrEqual(property, parsedValue));
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
