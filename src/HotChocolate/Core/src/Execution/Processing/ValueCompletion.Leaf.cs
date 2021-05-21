using System;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing
{
    internal static partial class ValueCompletion
    {
        private static bool TryCompleteLeafValue(
            IOperationContext operationContext,
            MiddlewareContext resolverContext,
            ISelection selection,
            Path path,
            IType fieldType,
            object? result,
            out object? completedResult)
        {
            try
            {
                var leafType = (ILeafType)fieldType;
                Type runtimeType = leafType.RuntimeType;

                if (!runtimeType.IsInstanceOfType(result) &&
                    operationContext.Converter.TryConvert(runtimeType, result, out var c))
                {
                    result = c;
                }

                completedResult = leafType.Serialize(result);
                return true;
            }
            catch (SerializationException ex)
            {
                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    InvalidLeafValue(ex, selection.SyntaxNode, path));
            }
            catch (Exception ex)
            {
                ReportError(
                    operationContext,
                    resolverContext,
                    selection,
                    UnexpectedLeafValueSerializationError(
                        ex,
                        operationContext.ErrorHandler,
                        selection.SyntaxNode,
                        path));
            }

            completedResult = null;
            return true;
        }
    }
}
