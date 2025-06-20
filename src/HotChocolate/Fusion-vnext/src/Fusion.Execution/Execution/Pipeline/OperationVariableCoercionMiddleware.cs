using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private static readonly Dictionary<string, object?> s_empty = [];
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    public OperationVariableCoercionMiddleware(
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _diagnosticEvents = diagnosticEvents;
    }

    public ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next)
    {
        var operationExecutionPlan = context.GetOperationExecutionPlan();

        if (operationExecutionPlan is null)
        {
            context.Result = ErrorHelper.StateInvalidForVariableCoercion();
            return default;
        }

        return TryCoerceVariables(
            context,
            operationExecutionPlan.Operation.VariableDefinitions,
            _diagnosticEvents)
            ? next(context)
            : default;
    }

    public static bool TryCoerceVariables(
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
                if(VariableCoercionHelper.TryCoerceVariableValues(
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues ?? s_empty,
                    out var coercedValues,
                    out var error))
                {
                    context.VariableValues = [new VariableValueCollection(coercedValues)];
                    return true;
                }
                else
                {
                    context.Result = OperationResultBuilder.CreateError(error);
                    return false;
                }
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
                    if(VariableCoercionHelper.TryCoerceVariableValues(
                        context.Schema,
                        variableDefinitions,
                        variableSetInput[i],
                        out var coercedValues,
                        out var error))
                    {
                        variableSet[i] = new VariableValueCollection(coercedValues);
                    }
                    else
                    {
                        context.Result = OperationResultBuilder.CreateError(error);
                        return false;
                    }
                }

                context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(variableSet);
                return true;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }

    public static RequestMiddlewareConfiguration Create()
    {
        return new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new OperationVariableCoercionMiddleware(diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            nameof(OperationVariableCoercionMiddleware));
    }
}
