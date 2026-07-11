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
        => Complete(context, selection, selection.Type, resultValue, result);

    public static void Complete(
        ValueCompletionContext context,
        Selection selection,
        IType type,
        ResultElement resultValue,
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
            resultValue.SetNullValue();
            return;
        }

        switch (typeKind)
        {
            case TypeKind.Scalar or TypeKind.Enum:
                CompleteLeafValue(context, selection, (ILeafType)type, resultValue, result);
                break;

            case TypeKind.List:
                CompleteListValue(context, selection, type, resultValue, result);
                break;

            case TypeKind.Object or TypeKind.Interface or TypeKind.Union:
                CompleteCompositeValue(context, selection, type, resultValue, result);
                break;

            default:
                var error = UnexpectedValueCompletionError(selection, resultValue.Path);
                context.OperationContext.ReportError(error, context.ResolverContext);
                break;
        }
    }
}
