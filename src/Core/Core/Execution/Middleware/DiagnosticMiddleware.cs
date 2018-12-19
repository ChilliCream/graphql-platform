using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal sealed class DiagnosticMiddleware
    {
        private readonly QueryDelegate _next;

        public DiagnosticMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = QueryDiagnosticEvents.BeginExecute(
                context.Schema, context.Request);

            try
            {
                await _next(context);

                if (context.ValidationResult.HasErrors)
                {
                    QueryDiagnosticEvents.ValidationError(
                        context.Schema, context.Request,
                        context.Document, context.ValidationResult.Errors);
                }

                if (context.Exception != null)
                {
                    QueryDiagnosticEvents.QueryError(
                        context.Schema, context.Request,
                        context.Document, context.Exception);
                }
            }
            finally
            {
                QueryDiagnosticEvents.EndExecute(
                    activity, context.Schema,
                    context.Request, context.Document);
            }
        }
    }
}

