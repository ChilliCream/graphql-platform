using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class InstrumentationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    private InstrumentationMiddleware(RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        using (_diagnosticEvents.ExecuteRequest(context))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new InstrumentationMiddleware(next, diagnosticEvents);
                return context => middleware.InvokeAsync(context);
            },
            nameof(InstrumentationMiddleware));
}
