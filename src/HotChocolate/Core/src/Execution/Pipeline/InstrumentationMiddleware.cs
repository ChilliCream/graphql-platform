using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class InstrumentationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;

        public InstrumentationMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents)
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
    }
}
