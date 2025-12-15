using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    public static void Complete(
        ValueCompletionContext context,
        Selection selection,
        ResultElement resultValue,
        object? result)
        => Complete(context, selection, selection.Type, resultValue, index, result);

    public static void Complete(
        ValueCompletionContext context,
        Selection selection,
        IType type,
        ResultElement parent,
        object? result)
    {
        var typeKind = type.Kind;

        if (typeKind is TypeKind.NonNull)
        {
            type = type.InnerType();
            typeKind = type.Kind;
        }

        if (result is null)
        {
            parent.SetNullValue();
            return;
        }

        switch (typeKind)
        {
            case TypeKind.Scalar or TypeKind.Enum:
                CompleteLeafValue(context, selection, type, parent, index, result);
                break;

            case TypeKind.List:
                return CompleteListValue(context, selection, type, parent, index, result);

            case TypeKind.Object or TypeKind.Interface or TypeKind.Union:
                return CompleteCompositeValue(context, selection, type, parent, index, result);

            default:
            {
                var errorPath = CreatePathFromContext(selection, parent, index);
                var error = UnexpectedValueCompletionError(selection, errorPath);
                context.OperationContext.ReportError(error, context.ResolverContext, selection);
                return null;
            }
        }
    }
}
