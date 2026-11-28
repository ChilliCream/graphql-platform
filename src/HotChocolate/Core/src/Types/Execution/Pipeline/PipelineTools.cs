using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal static class PipelineTools
{
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];

    public static void CoerceVariables(
        RequestContext context,
        VariableCoercionHelper coercionHelper,
        VariableCoercionPlanCache coercionPlans,
        OperationDefinitionNode operation,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        var variableDefinitions = operation.VariableDefinitions;

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
                var plan = coercionPlans.GetOrCreate(operation, context.Schema);
                var coercedValues = new Dictionary<string, Processing.VariableValue>(variableDefinitions.Count);

                coercionHelper.CoerceVariableValues(
                    plan,
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
                var plan = coercionPlans.GetOrCreate(operation, context.Schema);
                var variableValueSets = variableBatchRequest.VariableValues.Document.RootElement;
                var variableSet = new IVariableValueCollection[variableValueSets.GetArrayLength()];
                var i = 0;

                foreach (var variableValues in variableValueSets.EnumerateArray())
                {
                    var coercedValues = new Dictionary<string, Processing.VariableValue>(variableDefinitions.Count);

                    coercionHelper.CoerceVariableValues(
                        plan,
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
