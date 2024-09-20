using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class InstrumentationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    private InstrumentationMiddleware(RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        using (_diagnosticEvents.ExecuteRequest(context))
        {
            await _next(context).ConfigureAwait(false);

            if (context.Exception is { } exception)
            {
                _diagnosticEvents.RequestError(context, exception);
            }
        }
    }

    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var middleware = new InstrumentationMiddleware(next, diagnosticEvents);
            return context => middleware.InvokeAsync(context);
        };
}
