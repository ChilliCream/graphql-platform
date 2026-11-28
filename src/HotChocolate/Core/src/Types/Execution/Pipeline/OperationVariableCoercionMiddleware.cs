using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Pipeline.PipelineTools;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly VariableCoercionHelper _coercionHelper;
    private readonly VariableCoercionPlanCache _coercionPlans;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private OperationVariableCoercionMiddleware(
        RequestDelegate next,
        VariableCoercionHelper coercionHelper,
        VariableCoercionPlanCache coercionPlans,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(coercionHelper);
        ArgumentNullException.ThrowIfNull(coercionPlans);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _next = next;
        _coercionHelper = coercionHelper;
        _coercionPlans = coercionPlans;
        _diagnosticEvents = diagnosticEvents;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var operation = context.GetNormalizedOperation();

        CoerceVariables(
            context,
            _coercionHelper,
            _coercionPlans,
            operation,
            _diagnosticEvents);

        await _next(context).ConfigureAwait(false);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var coercionHelper = core.Services.GetRequiredService<VariableCoercionHelper>();
                var coercionPlans = core.SchemaServices.GetRequiredService<VariableCoercionPlanCache>();
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var middleware = new OperationVariableCoercionMiddleware(
                    next,
                    coercionHelper,
                    coercionPlans,
                    diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.OperationVariableCoercionMiddleware);
}
