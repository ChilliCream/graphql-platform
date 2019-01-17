using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

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
            DiagnosticListenerInitializer initializer = context.Services
                .GetRequiredService<DiagnosticListenerInitializer>();

            initializer.Start();

            Activity activity = QueryDiagnosticEvents.BeginExecute(context);

            try
            {
                await _next(context).ConfigureAwait(false);

                if (context.Exception != null)
                {
                    QueryDiagnosticEvents.QueryError(context);
                }
            }
            finally
            {
                QueryDiagnosticEvents.EndExecute(activity, context);
            }
        }
    }
}
