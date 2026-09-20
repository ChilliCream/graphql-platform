using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Language.GraphQLCharacters;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    // The two '-' separators, the optional '+' before the operation name and the
    // at most 20 digits of the ulong executor version.
    private const int CacheIdSeparatorLength = 23;

    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];

    public static string CreateCacheId(this RequestContext context)
    {
        var documentId = context.GetOperationDocumentId();

        if (documentId.IsEmpty)
        {
            throw new ArgumentException(
                "The request context must have a valid document ID "
                + "in order to create a cache ID.");
        }

        var schemaName = context.Schema.Name;
        var documentIdValue = documentId.Value;
        var operationName = context.Request.OperationName;
        var maxLength =
            schemaName.Length
            + documentIdValue.Length
            + (operationName?.Length ?? 0)
            + CacheIdSeparatorLength;

        char[]? rented = null;
        var buffer = maxLength <= StackallocThreshold
            ? stackalloc char[maxLength]
            : rented = ArrayPool<char>.Shared.Rent(maxLength);

        try
        {
            schemaName.CopyTo(buffer);
            var length = schemaName.Length;
            buffer[length++] = '-';

            context.ExecutorVersion.TryFormat(buffer[length..], out var versionLength);
            length += versionLength;
            buffer[length++] = '-';

            documentIdValue.CopyTo(buffer[length..]);
            length += documentIdValue.Length;

            if (operationName is not null)
            {
                buffer[length++] = '+';
                operationName.CopyTo(buffer[length..]);
                length += operationName.Length;
            }

            return new string(buffer[..length]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    public static void CoerceVariables(
        RequestContext context,
        VariableCoercionHelper coercionHelper,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        if (context.VariableValues.Length > 0)
        {
            return;
        }

        if (variableDefinitions.Count == 0)
        {
            context.VariableValues = s_noVariables;
            return;
        }

        if (context.Request is OperationRequest operationRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var coercedValues = new Dictionary<string, Processing.VariableValue>();

                coercionHelper.CoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues?.Document.RootElement ?? default,
                    coercedValues,
                    context);

                context.VariableValues = [new VariableValueCollection(coercedValues)];
                return;
            }
        }

        if (context.Request is VariableBatchRequest variableBatchRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var schema = context.Schema;
                var variableValueSets = variableBatchRequest.VariableValues.Document.RootElement;
                var variableSet = new IVariableValueCollection[variableValueSets.GetArrayLength()];
                var i = 0;

                foreach (var variableValues in variableValueSets.EnumerateArray())
                {
                    var coercedValues = new Dictionary<string, Processing.VariableValue>();

                    coercionHelper.CoerceVariableValues(
                        schema,
                        variableDefinitions,
                        variableValues,
                        coercedValues,
                        context);

                    variableSet[i++] = new VariableValueCollection(coercedValues);
                }

                context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(variableSet);
                return;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }
}
