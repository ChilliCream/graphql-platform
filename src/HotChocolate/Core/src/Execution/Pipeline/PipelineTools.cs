using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly Dictionary<string, object?> _empty = new();

    private static readonly IReadOnlyList<VariableValueCollection> _noVariables = [VariableValueCollection.Empty,];

    public static string CreateOperationId(string documentId, string? operationName)
        => operationName is null
            ? documentId
            : $"{documentId}+{operationName}";

    public static string CreateCacheId(this IRequestContext context)
    {
        var documentId = context.DocumentId!.Value.Value;
        var operationName = context.Request.OperationName;

        if (string.IsNullOrEmpty(documentId))
        {
            throw new ArgumentException(
                "The request context must have a valid document ID "
                + "in order to create a cache ID.");
        }

        var operationId = CreateOperationId(documentId, operationName);

        return $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";
    }

    public static IReadOnlyList<IVariableValueCollection> CoerceVariables(
        IRequestContext context,
        VariableCoercionHelper coercionHelper,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions)
    {
        if (context.Variables is not null)
        {
            return context.Variables;
        }

        if (variableDefinitions.Count == 0)
        {
            context.Variables = _noVariables;
            return _noVariables;
        }

        if (context.Request is OperationRequest operationRequest)
        {
            using (context.DiagnosticEvents.CoerceVariables(context))
            {
                var coercedValues = new Dictionary<string, VariableValueOrLiteral>();

                coercionHelper.CoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues ?? _empty,
                    coercedValues);

                context.Variables = new[] { new VariableValueCollection(coercedValues), };
                return context.Variables;
            }
        }

        if (context.Request is VariableBatchRequest variableBatchRequest)
        {
            using (context.DiagnosticEvents.CoerceVariables(context))
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

                context.Variables = variableSet;
                return context.Variables;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }
}
