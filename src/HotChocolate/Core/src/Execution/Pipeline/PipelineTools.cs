using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal static class PipelineTools
    {
        private static readonly IReadOnlyDictionary<string, object?> _empty =
            new Dictionary<string, object?>();

        public static string CreateOperationId(string documentId, string? operationName) =>
            operationName is null ? documentId : $"{documentId}+{operationName}";

        public static string CreateCacheId(this IRequestContext context, string operationId) =>
            $"{context.Schema.Name}-{context.ExecutorVersion}-{operationId}";

        public static void CoerceVariables(
            IRequestContext context,
            VariableCoercionHelper coercionHelper,
            IReadOnlyList<VariableDefinitionNode> variableDefinitions)
        {
            if (variableDefinitions.Count == 0)
            {
                context.Variables = VariableValueCollection.Empty;
            }
            else
            {
                var coercedValues = new Dictionary<string, VariableValueOrLiteral>();

                coercionHelper.CoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    context.Request.VariableValues ?? _empty,
                    coercedValues);

                context.Variables = new VariableValueCollection(coercedValues);
            }
        }
    }
}
