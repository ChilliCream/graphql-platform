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
        private readonly DiagnosticSource _source;

        public InstrumentationMiddleware(
            QueryDelegate next,
            DiagnosticSource source)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _source = source ??
                throw new ArgumentNullException(nameof(source));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = _source.BeginExecute(context);

            try
            {
                await _next(context).ConfigureAwait(false);

                if (context.Exception != null)
                {
                    _source.QueryError(context);
                }
            }
            finally
            {
                _source.EndExecute(activity, context);
            }
        }
    }
}
