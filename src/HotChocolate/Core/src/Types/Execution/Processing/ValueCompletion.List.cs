using System.Collections;
using System.Text.Json;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.ErrorHelper;

namespace HotChocolate.Execution.Processing;

internal static partial class ValueCompletion
{
    private static void CompleteListValue(
        ValueCompletionContext context,
        Selection selection,
        IType type,
        ResultElement resultValue,
        object runtimeValue)
    {
        var elementType = type.InnerType();

        if (runtimeValue is Array array)
        {
            var i = 0;

            resultValue.SetArrayValue(array.Length);

            foreach (var element in resultValue.EnumerateArray())
            {
                Complete(
                    context,
                    selection,
                    elementType,
                    element,
                    array.GetValue(i++));

                // if we ran into an error that invalidated the result we abort.
                if (element.IsInvalidated)
                {
                    return;
                }
            }

            return;
        }

        if (runtimeValue is IList list)
        {
            var i = 0;

            resultValue.SetArrayValue(list.Count);

            foreach (var element in resultValue.EnumerateArray())
            {
                Complete(
                    context,
                    selection,
                    elementType,
                    element,
                    list[i++]);

                // if we ran into an error that invalidated the result we abort.
                if (element.IsInvalidated)
                {
                    return;
                }
            }

            return;
        }

        if (runtimeValue is JsonElement { ValueKind: JsonValueKind.Array } node)
        {
            resultValue.SetArrayValue(node.GetArrayLength());

            using var runtimeEnumerator = node.EnumerateArray().GetEnumerator();

            foreach (var element in resultValue.EnumerateArray())
            {
                runtimeEnumerator.MoveNext();

                Complete(
                    context,
                    selection,
                    elementType,
                    element,
                    runtimeEnumerator.Current);

                // if we ran into an error that invalidated the result we abort.
                if (element.IsInvalidated)
                {
                    return;
                }
            }

            return;
        }

        if (runtimeValue is IEnumerable enumerable)
        {
            var i = 0;
            var temp = new List<object?>();

            foreach (var value in enumerable)
            {
                temp.Add(value);
            }

            resultValue.SetArrayValue(temp.Count);

            foreach (var element in resultValue.EnumerateArray())
            {
                Complete(
                    context,
                    selection,
                    elementType,
                    element,
                    temp[i++]);

                // if we ran into an error that invalidated the result we abort.
                if (element.IsInvalidated)
                {
                    return;
                }
            }

            return;
        }

        var operationContext = context.OperationContext;
        var resolverContext = context.ResolverContext;
        var error = ListValueIsNotSupported(runtimeValue.GetType(), selection, resultValue.Path);
        operationContext.ReportError(error, resolverContext);
    }

    internal static void PropagateNullValues(ResultElement result)
    {
        result.SetNullValue();

        do
        {
            result = result.Parent;

            if (result.IsNullable)
            {
                result.SetNullValue();
                return;
            }

            result.Invalidate();
        } while (result.Parent is { ValueKind: not JsonValueKind.Undefined, IsInvalidated: false });
    }
}
