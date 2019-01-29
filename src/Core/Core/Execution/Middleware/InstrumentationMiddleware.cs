using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution
{
    internal sealed class InstrumentationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;

        public InstrumentationMiddleware(
            QueryDelegate next,
            QueryExecutionDiagnostics diagnosticEvents)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents
                ?? throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = _diagnosticEvents.BeginQuery(context);

            try
            {
                await _next(context).ConfigureAwait(false);

                if (context.Exception != null)
                {
                    _diagnosticEvents.QueryError(context);
                }
            }
            finally
            {
                _diagnosticEvents.EndQuery(activity, context);
            }
        }
    }
}
