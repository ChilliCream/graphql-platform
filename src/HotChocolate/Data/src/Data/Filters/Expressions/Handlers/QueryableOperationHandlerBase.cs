using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableOperationHandlerBase
        : FilterFieldHandler<Expression, QueryableFilterContext>
    {
        public override bool TryHandleOperation(
            QueryableFilterContext context,
            IFilterInputType type,
            IFilterOperationField field,
            IValueNode value,
            [NotNullWhen(true)] out Expression result)
        {
            object parsedValue = field.Type.ParseLiteral(value);

            if (!field.IsNullable && parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, value, context));

                result = null!;
                return false;
            }

            if (field.Type.IsInstanceOfType(value))
            {
                Expression property = context.GetInstance();
                result = FilterExpressionBuilder.Equals(property, parsedValue);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }

        }

        public abstract Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType type,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue);
    }
}
