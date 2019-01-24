using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution
{
    internal sealed class InstrumentationMiddleware
    {
        private readonly QueryDelegate _next;

        public InstrumentationMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = DiagnosticEvents.BeginQuery(context);

            try
            {
                await _next(context).ConfigureAwait(false);

                if (context.Exception != null)
                {
                    DiagnosticEvents.QueryError(context);
                }
            }
            finally
            {
                DiagnosticEvents.EndQuery(activity, context);
            }
        }
    }
}
