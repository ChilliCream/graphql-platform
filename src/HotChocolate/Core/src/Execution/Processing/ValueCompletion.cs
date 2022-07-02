using System.Collections.Generic;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    public static bool TryComplete(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result,
        List<ResolverTask> bufferedTasks,
        out object? completedResult)
    {
        var typeKind = fieldType.Kind;
        var nonNull = false;

        if (typeKind is TypeKind.NonNull)
        {
            nonNull = true;
            fieldType = fieldType.InnerType();
            typeKind = fieldType.Kind;
        }

        if (result is null)
        {
            completedResult = null;
            return !nonNull;
        }

        if (typeKind is TypeKind.List)
        {
            return TryCompleteListValue(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                responseName,
                responseIndex,
                result,
                bufferedTasks,
                out completedResult);
        }

        if (typeKind is TypeKind.Scalar or TypeKind.Enum)
        {
            return TryCompleteLeafValue(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                result,
                out completedResult);
        }

        if (typeKind is TypeKind.Object or TypeKind.Interface or TypeKind.Union)
        {
            return TryCompleteCompositeValue(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType,
                result,
                bufferedTasks,
                out completedResult);
        }

        ReportError(
            operationContext,
            resolverContext,
            selection,
            UnexpectedValueCompletionError(selection.SyntaxNode, path));

        completedResult = null;
        return false;
    }

}
