using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableOperationHandlerBase
        : FilterOperationHandler<Expression, QueryableFilterContext>
    {
        public override bool TryHandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            ObjectFieldNode node,
            [NotNullWhen(true)] out Expression result)
        {
            IValueNode? value = node.Value;
            var parsedValue = field.Type.ParseLiteral(value);

            if (!field.IsNullable && parsedValue == null)
            {
                context.ReportError(
                    ErrorHelper.CreateNonNullError(field, value, context));

                result = null!;
                return false;
            }

            if (field.Type.IsInstanceOfType(value))
            {
                result = HandleOperation(
                    context, declaringType, field, fieldType, value, parsedValue);
                return true;
            }
            else
            {
                throw new InvalidOperationException();
            }

        }

        public abstract Expression HandleOperation(
            QueryableFilterContext context,
            IFilterInputType declaringType,
            IFilterOperationField field,
            IType fieldType,
            IValueNode value,
            object parsedValue);
    }
}
