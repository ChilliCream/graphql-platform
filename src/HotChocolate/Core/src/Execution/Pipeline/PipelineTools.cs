using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly Dictionary<string, object?> _empty = new();

    private static readonly IReadOnlyList<VariableValueCollection> _noVariables = [VariableValueCollection.Empty,];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateOperationId(string documentId, string? operationName)
        => operationName is null
            ? documentId
            : $"{documentId}+{operationName}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateCacheId(this IRequestContext context, string operationId)
        => $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateCacheId(
        this IRequestContext context,
        string documentId,
        string? operationName)
        => CreateCacheId(context, CreateOperationId(documentId, operationName));

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