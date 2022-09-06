using System;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static object? CompleteLeafValue(
        OperationContext operationContext,
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
            var error = InvalidLeafValue(ex, selection.SyntaxNode, path);
            operationContext.ReportError(error, resolverContext, selection);
        }
        catch (Exception ex)
        {
            var error = UnexpectedLeafValueSerializationError(
                ex,
                operationContext.ErrorHandler,
                selection.SyntaxNode,
                path);
            operationContext.ReportError(error, resolverContext, selection);
        }

        return null;
    }
}
