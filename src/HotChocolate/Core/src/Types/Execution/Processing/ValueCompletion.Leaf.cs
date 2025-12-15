using HotChocolate.Text.Json;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static void CompleteLeafValue(
        ValueCompletionContext context,
        Selection selection,
        ILeafType2 type,
        ResultElement resultValue,
        object? runtimeValue)
    {
        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;

        try
        {
            var runtimeType = type.ToRuntimeType();

            if (!runtimeType.IsInstanceOfType(runtimeValue)
                && operationContext.Converter.TryConvert(runtimeType, runtimeValue, out var c))
            {
                runtimeValue = c;
            }

            type.Serialize(runtimeValue, resultValue);
            return;
        }
        catch (SerializationException ex)
        {
            var errorPath = resultValue.Path;
            var error = InvalidLeafValue(ex, selection, errorPath);
            operationContext.ReportError(error, resolverContext, selection);
        }
        catch (Exception ex)
        {
            var errorPath = resultValue.Path;
            var error = UnexpectedLeafValueSerializationError(ex, selection, errorPath);
            operationContext.ReportError(error, resolverContext, selection);
        }

        resultValue.SetNullValue();
    }
}
