using System;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static object? CompleteLeafValue(
        IOperationContext operationContext,
        MiddlewareContext resolverContext,
        ISelection selection,
        Path path,
        IType fieldType,
        object? result)
    {
        try
        {
            var leafType = (ILeafType)fieldType;
            var runtimeType = leafType.RuntimeType;

            if (!runtimeType.IsInstanceOfType(result) &&
                operationContext.Converter.TryConvert(runtimeType, result, out var c))
            {
                result = c;
            }

            return leafType.Serialize(result);
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

        return null;
    }
}
