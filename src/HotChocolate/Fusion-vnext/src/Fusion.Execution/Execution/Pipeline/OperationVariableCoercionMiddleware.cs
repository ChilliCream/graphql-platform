using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    private OperationVariableCoercionMiddleware(
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        _diagnosticEvents = diagnosticEvents;
    }

    public ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next)
    {
        var operationExecutionPlan = context.GetOperationPlan();

        if (operationExecutionPlan is null)
        {
            context.Result = ErrorHelper.StateInvalidForVariableCoercion();
            return default;
        }

        return TryCoerceVariables(
            context,
            operationExecutionPlan.VariableDefinitions,
            _diagnosticEvents)
            ? next(context)
            : default;
    }

    private static bool TryCoerceVariables(
        RequestContext context,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        if (context.VariableValues.Length > 0)
        {
            return true;
        }

        if (variableDefinitions.Count == 0)
        {
            context.VariableValues = s_noVariables;
            return true;
        }

        if (context.Request is OperationRequest operationRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                if (VariableCoercionHelper.TryCoerceVariableValues(
                    context,
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues?.Document.RootElement ?? default,
                    out var coercedValues,
                    out var error))
                {
                    context.VariableValues = [new VariableValueCollection(coercedValues)];
                    return true;
                }

                context.Result = OperationResult.FromError(error);
                return false;
            }
        }

        if (context.Request is VariableBatchRequest variableBatchRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var variableValuesSetInput = variableBatchRequest.VariableValues.Document.RootElement;
                var variableValuesSet = new IVariableValueCollection[variableValuesSetInput.GetArrayLength()];
                var i = 0;

                foreach (var variableValuesInput in variableValuesSetInput.EnumerateArray())
                {
                    if (VariableCoercionHelper.TryCoerceVariableValues(
                        context,
                        context.Schema,
                        variableDefinitions,
                        variableValuesInput,
                        out var coercedValues,
                        out var error))
                    {
                        variableValuesSet[i++] = new VariableValueCollection(coercedValues);
                    }
                    else
                    {
                        context.Result = OperationResult.FromError(error);
                        return false;
                    }
                }

                context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(variableValuesSet);
                return true;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new OperationVariableCoercionMiddleware(diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.OperationVariableCoercionMiddleware);
}
