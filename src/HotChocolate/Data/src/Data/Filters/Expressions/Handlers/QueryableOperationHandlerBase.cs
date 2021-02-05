using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableOperationHandlerBase
        : FilterOperationHandler<QueryableFilterContext, Expression>
    {
        public override bool TryHandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out Expression result)
        {
            IValueNode value = node.Value;
            object? parsedValue = field.Type.ParseLiteral(value);
            parsedValue = field.Formatter is not null
                ? field.Formatter.OnAfterDeserialize(parsedValue)
                : parsedValue;

            if ((!context.RuntimeTypes.Peek().IsNullable || !CanBeNull) && parsedValue is null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, value, context));

                result = null!;
                return false;
            }

            if (field.Type.IsInstanceOfType(value))
            {
                result = HandleOperation(
                    context, field, value, parsedValue);

                return true;
            }

            throw new InvalidOperationException();
        }

        protected bool CanBeNull { get; set; } = true;

        public abstract Expression HandleOperation(
            QueryableFilterContext context,
            IFilterOperationField field,
            IValueNode value,
            object parsedValue);
    }
}
