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
        TypeKind typeKind = fieldType.Kind;

        if (typeKind is TypeKind.NonNull)
        {
            return TryComplete(
                operationContext,
                resolverContext,
                selection,
                path,
                fieldType.InnerType(),
                responseName,
                responseIndex,
                result,
                bufferedTasks,
                out completedResult) &&
                completedResult is not null;
        }

        if (result is null)
        {
            completedResult = null;
            return true;
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
