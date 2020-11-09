using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.MongoDb.Data.Filters
{
    public abstract class MongoDbOperationHandlerBase
        : FilterOperationHandler<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
    {
        public override bool TryHandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out MongoDbFilterDefinition result)
        {
            IValueNode value = node.Value;
            object? parsedValue = field.Type.ParseLiteral(value);

            if ((!context.RuntimeTypes.Peek().IsNullable || !CanBeNull) &&
                parsedValue is null)
            {
                context.ReportError(ErrorHelper.CreateNonNullError(field, value, context));

                result = null!;
                return false;
            }

            if (field.Type.IsInstanceOfType(value))
            {
                result = HandleOperation(
                    context,
                    field,
                    value,
                    parsedValue);

                return true;
            }

            throw new InvalidOperationException();
        }

        protected bool CanBeNull { get; set; } = true;

        public abstract MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue);
    }
}
