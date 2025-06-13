using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];

    public ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next)
    {
        return next(context);
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

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (factoryContext, next) =>
            {
                var middleware = new OperationVariableCoercionMiddleware();
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            nameof(OperationVariableCoercionMiddleware));
    }
}