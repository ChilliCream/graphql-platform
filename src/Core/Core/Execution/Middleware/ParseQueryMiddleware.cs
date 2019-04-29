using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class ParseQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryParser _parser;
        private readonly Cache<DocumentNode> _queryCache;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;

        public ParseQueryMiddleware(
            QueryDelegate next,
            IQueryParser parser,
            Cache<DocumentNode> queryCache,
            QueryExecutionDiagnostics diagnosticEvents)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _parser = parser
                ?? throw new ArgumentNullException(nameof(parser));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
            _diagnosticEvents = diagnosticEvents
                ?? throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources
                            .ParseQueryMiddleware_InComplete)
                        .Build());
            }
            else
            {
                Activity activity = _diagnosticEvents.BeginParsing(context);

                try
                {
                    bool documentRetrievedFromCache = true;

                    context.Document = _queryCache.GetOrCreate(
                        context.Request.Query,
                        () =>
                        {
                            documentRetrievedFromCache = false;
                            return ParseDocument(context.Request.Query);
                        });

                    context.ContextData[ContextDataKeys.DocumentCached] =
                        documentRetrievedFromCache;
                }
                finally
                {
                    _diagnosticEvents.EndParsing(activity, context);
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        private DocumentNode ParseDocument(string queryText)
        {
            return _parser.Parse(queryText);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request == null ||
                context.Request.Query == null;
        }
    }
}

