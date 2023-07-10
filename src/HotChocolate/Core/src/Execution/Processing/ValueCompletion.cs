using System.Collections.Generic;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    public static object? Complete(
        OperationContext operationContext,
        MiddlewareContext resolverContext,
        List<ResolverTask> tasks,
        ISelection selection,
        Path path,
        IType fieldType,
        string responseName,
        int responseIndex,
        object? result)
    {
        var typeKind = fieldType.Kind;
        if (typeKind is TypeKind.NonNull)
        {
            fieldType = fieldType.InnerType();
            typeKind = fieldType.Kind;
        }

        if (result is null)
        {
            return null;
        }

        switch (typeKind)
        {
            case TypeKind.Scalar:
            case TypeKind.Enum:
                return CompleteLeafValue(
                    operationContext,
                    resolverContext,
                    selection,
                    path,
                    fieldType,
                    result);

            case TypeKind.List:
                return CompleteListValue(
                    operationContext,
                    resolverContext,
                    tasks,
                    selection,
                    path,
                    fieldType,
                    responseName,
                    responseIndex,
                    result);

            case TypeKind.Object:
            case TypeKind.Interface:
            case TypeKind.Union:
                return CompleteCompositeValue(
                    operationContext,
                    resolverContext,
                    tasks,
                    selection,
                    path,
                    fieldType,
                    result);

            default:
            {
                var error = UnexpectedValueCompletionError(selection.SyntaxNode, path);
                operationContext.ReportError(error, resolverContext, selection);
                return null;
            }
        }
    }
}
