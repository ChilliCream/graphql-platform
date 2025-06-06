using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly Dictionary<string, object?> s_empty = [];
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

    public static IReadOnlyList<IVariableValueCollection> CoerceVariables(
        RequestContext context,
        VariableCoercionHelper coercionHelper,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        if (context.VariableValues.Length > 0)
        {
            return context.VariableValues;
        }

        if (variableDefinitions.Count == 0)
        {
            context.VariableValues = s_noVariables;
            return s_noVariables;
        }

        if (context.Request is OperationRequest operationRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var coercedValues = new Dictionary<string, VariableValueOrLiteral>();

                coercionHelper.CoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues ?? s_empty,
                    coercedValues);

                context.VariableValues = [new VariableValueCollection(coercedValues)];
                return context.VariableValues;
            }
        }

        if (context.Request is VariableBatchRequest variableBatchRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var schema = context.Schema;
                var variableSetCount = variableBatchRequest.VariableValues?.Count ?? 0;
                var variableSetInput = variableBatchRequest.VariableValues!;
                var variableSet = new IVariableValueCollection[variableSetCount];

                for (var i = 0; i < variableSetCount; i++)
                {
                    var coercedValues = new Dictionary<string, VariableValueOrLiteral>();

                    coercionHelper.CoerceVariableValues(
                        schema,
                        variableDefinitions,
                        variableSetInput[i],
                        coercedValues);

                    variableSet[i] = new VariableValueCollection(coercedValues);
                }

                context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(variableSet);
                return context.VariableValues;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }
}
