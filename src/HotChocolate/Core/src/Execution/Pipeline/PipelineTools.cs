using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly Dictionary<string, object?> _empty = new();
    private static readonly VariableValueCollection _noVariables = VariableValueCollection.Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateOperationId(string documentId, string? operationName)
        => operationName is null ? documentId : $"{documentId}+{operationName}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateCacheId(this IRequestContext context, string operationId)
        => $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateCacheId(
        this IRequestContext context,
        string documentId,
        string? operationName)
        => CreateCacheId(context, CreateOperationId(documentId, operationName));

    public static IVariableValueCollection CoerceVariables(
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
        else
        {
            using (context.DiagnosticEvents.CoerceVariables(context))
            {
                var coercedValues = new Dictionary<string, VariableValueOrLiteral>();

                coercionHelper.CoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    context.Request.VariableValues ?? _empty,
                    coercedValues);

                context.Variables = new VariableValueCollection(coercedValues);
                return context.Variables;
            }
        }
    }
}
