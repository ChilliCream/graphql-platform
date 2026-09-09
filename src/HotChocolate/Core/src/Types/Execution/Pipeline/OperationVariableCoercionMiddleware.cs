using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly VariableCoercionHelper _coercionHelper;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly ICostValidationVariableCoercionPolicy? _costValidationPolicy;

    private OperationVariableCoercionMiddleware(
        RequestDelegate next,
        VariableCoercionHelper coercionHelper,
        IExecutionDiagnosticEvents diagnosticEvents,
        ICostValidationVariableCoercionPolicy? costValidationPolicy)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(coercionHelper);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _coercionHelper = coercionHelper;
        _diagnosticEvents = diagnosticEvents;
        _costValidationPolicy = costValidationPolicy;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        if (context.TryGetOperation(out var operation))
        {
            if (!context.IsWarmupRequest()
                && !IsCostValidationWithoutVariables(context, _costValidationPolicy))
            {
                CoerceVariables(
                    context,
                    _coercionHelper,
                    operation.Definition.VariableDefinitions,
                    _diagnosticEvents);
            }

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationVariableCoercion();
        }
    }

    private static bool IsCostValidationWithoutVariables(
        RequestContext context,
        ICostValidationVariableCoercionPolicy? costValidationPolicy)
        => context.Request is OperationRequest operationRequest
            && context.ContextData.ContainsKey(ExecutionContextData.ValidateCost)
            && HasNoVariableValues(operationRequest.VariableValues)
            && costValidationPolicy?.SkipVariableCoercion(context) is true;

    private static bool HasNoVariableValues(JsonDocumentOwner? variableValues)
    {
        if (variableValues is null)
        {
            return true;
        }

        var root = variableValues.Document.RootElement;

        if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return true;
        }

        if (root.ValueKind is not JsonValueKind.Object)
        {
            return false;
        }

#if NET10_0_OR_GREATER
        return root.GetPropertyCount() == 0;
#else
        return !root.EnumerateObject().MoveNext();
#endif
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var coercionHelper = core.Services.GetRequiredService<VariableCoercionHelper>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var costValidationPolicy = core.Features.Get<ICostValidationVariableCoercionPolicy>();
                var middleware = new OperationVariableCoercionMiddleware(
                    next,
                    coercionHelper,
                    diagnosticEvents,
                    costValidationPolicy);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OperationVariableCoercionMiddleware);
}

internal interface ICostValidationVariableCoercionPolicy
{
    bool SkipVariableCoercion(RequestContext context);
}
