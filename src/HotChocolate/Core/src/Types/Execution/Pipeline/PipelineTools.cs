using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];

    public static string CreateOperationId(string documentId, string? operationName)
        => operationName is null
            ? documentId
            : $"{documentId}+{operationName}";

    public static string CreateCacheId(this RequestContext context)
    {
        var documentId = context.GetOperationDocumentId();
        var operationName = context.Request.OperationName;

        if (documentId.IsEmpty)
        {
            throw new ArgumentException(
                "The request context must have a valid document ID "
                + "in order to create a cache ID.");
        }

        var operationId = CreateOperationId(documentId.Value, operationName);

        return $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";
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
