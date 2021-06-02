using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing
{
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
}
