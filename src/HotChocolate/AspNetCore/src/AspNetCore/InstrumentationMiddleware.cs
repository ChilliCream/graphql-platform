using Microsoft.AspNetCore.Http;
using HotChocolate.Execution.Instrumentation;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class InstrumentationMiddleware
{
    private readonly HttpRequestDelegate _next;
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public InstrumentationMiddleware(
        HttpRequestDelegate next, 
        IServerDiagnosticEvents diagnosticEvents)
    {
        _next = next ?? 
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ?? 
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (_diagnosticEvents.ExecuteHttpRequest(context))
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                _diagnosticEvents.CancelHttpRequest(context);
            }
            catch (Exception ex)
            {

                _diagnosticEvents.ServerRequestError(context, ex);
                throw;
            }
        }
    }
}
