using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly VariableCoercionHelper _coercionHelper;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private OperationVariableCoercionMiddleware(
        RequestDelegate next,
        VariableCoercionHelper coercionHelper,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(coercionHelper);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _coercionHelper = coercionHelper;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        if (context.TryGetOperation(out var operation))
        {
            CoerceVariables(
                context,
                _coercionHelper,
                operation.Definition.VariableDefinitions,
                _diagnosticEvents);

            await _next(context).ConfigureAwait(false);
        }
        else
        {
            context.Result = ErrorHelper.StateInvalidForOperationVariableCoercion();
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var coercionHelper = core.Services.GetRequiredService<VariableCoercionHelper>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var middleware = new OperationVariableCoercionMiddleware(next, coercionHelper, diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            nameof(OperationVariableCoercionMiddleware));
}
