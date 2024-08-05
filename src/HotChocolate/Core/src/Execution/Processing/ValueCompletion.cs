using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.PathHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    public static object? Complete(
        ValueCompletionContext context,
        ISelection selection,
        ResultData parent,
        int index,
        object? result)
        => Complete( context, selection, selection.Type, parent, index, result);

    public static object? Complete(
        ValueCompletionContext context,
        ISelection selection,
        IType type,
        ResultData parent,
        int index,
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
            return null;
        }

        if (typeKind is TypeKind.Scalar or TypeKind.Enum)
        {
            return CompleteLeafValue(context, selection, type, parent, index, result);
        }

        if (typeKind is TypeKind.List)
        {
            return CompleteListValue(context, selection, type, parent, index, result);
        }

        if (typeKind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
        {
            return CompleteCompositeValue(context, selection, type, parent, index, result);
        }

        var errorPath = CreatePathFromContext(selection, parent, index);
        var error = UnexpectedValueCompletionError(selection.SyntaxNode, errorPath);
        context.OperationContext.ReportError(error, context.ResolverContext, selection);
        return null;
    }
}
