using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class ParseQueryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;

        public ParseQueryMiddleware(
            RequestDelegate next,
            QueryExecutionDiagnostics diagnosticEvents)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        public async Task InvokeAsync(IRequestContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResultBuilder.CreateError(
                    ErrorBuilder.New()
                        .SetMessage("The parse query middleware expects a valid query request.")
                        .SetCode(ErrorCodes.Execution.Incomplete)
                        .Build());
            }
            else
            {
                Activity? activity = _diagnosticEvents.BeginParsing(context);

                try
                {
                    if (context.Document is null)
                    {
                        context.Document = ParseDocument(context.Request.Query);
                    }
                }
                finally
                {
                    _diagnosticEvents.EndParsing(activity, context);
                }

                await _next(context).ConfigureAwait(false);
            }
        }

        private DocumentNode ParseDocument(IQuery query)
        {
            if (query is QueryDocument parsed)
            {
                return parsed.Document;
            }

            if (query is QuerySourceText source)
            {
                return Utf8GraphQLParser.Parse(source.ToSpan());
            }

            throw new NotSupportedException(
                "The specified query type is not supported.");
        }

        private static bool IsContextIncomplete(IRequestContext context)
        {
            return context.Request is null
                || (context.Request.Query is null
                    && context.Request.QueryName is null);
        }
    }
}
