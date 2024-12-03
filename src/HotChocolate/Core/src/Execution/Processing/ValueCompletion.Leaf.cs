using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;
using static HotChocolate.Execution.Processing.PathHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static object? CompleteLeafValue(
        ValueCompletionContext context,
        ISelection selection,
        IType type,
        ResultData parent,
        int index,
        object? result)
    {
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;

        try
        {
            var leafType = (ILeafType)type;
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
            var errorPath = CreatePathFromContext(selection, parent, index);
            var error = InvalidLeafValue(ex, selection.SyntaxNode, errorPath);
            operationContext.ReportError(error, resolverContext, selection);
        }
        catch (Exception ex)
        {
            var errorPath = CreatePathFromContext(selection, parent, index);
            var error = UnexpectedLeafValueSerializationError(
                ex,
                operationContext.ErrorHandler,
                selection.SyntaxNode,
                errorPath);
            operationContext.ReportError(error, resolverContext, selection);
        }

        return null;
    }
}
