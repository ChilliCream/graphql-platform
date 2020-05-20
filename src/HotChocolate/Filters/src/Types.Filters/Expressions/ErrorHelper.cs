using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ErrorHelper
    {
        public static IError CreateNonNullError(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context) =>
                CreateNonNullError(field.Operation, field.Type, node.Value, context);

        public static IError CreateNonNullError(
            FilterOperation operation,
            IInputType type,
            IValueNode value,
            IFilterVisitorContext<Expression> context)
        {
            IFilterInputType filterType = context.Types.OfType<IFilterInputType>().First();

            return ErrorBuilder.New()
                .SetMessage(
                    "The provided value for filter `{0}` of type {1} is invalid. " +
                    "Null values are not supported.",
                    context.Operations.Peek().Name,
                    filterType.Visualize())
                .AddLocation(value)
                .SetExtension("expectedType", new NonNullType(type).Visualize())
                .SetExtension("filterKind", operation.FilterKind)
                .SetExtension("operationKind", operation.Kind)
                .SetExtension("filterType", filterType.Visualize())
                .Build();
        }
    }
}
