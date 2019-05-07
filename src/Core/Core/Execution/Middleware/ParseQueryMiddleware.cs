using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class ParseQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryParser _parser;
        private readonly Cache<ICachedQuery> _queryCache;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;

        public ParseQueryMiddleware(
            QueryDelegate next,
            IQueryParser parser,
            Cache<ICachedQuery> queryCache,
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
                // TODO : resources
                context.Result = QueryResult.CreateError(new QueryError(
                   "The parse query middleware expects " +
                   "a valid query request."));
            }
            else
            {
                Activity activity = _diagnosticEvents.BeginParsing(context);

                try
                {
                    bool documentRetrievedFromCache = true;

                    context.CachedQuery  = _queryCache.GetOrCreate(
                        context.Request.Query,
                        () =>
                        {
                            documentRetrievedFromCache = false;
                            DocumentNode document =
                                ParseDocument(context.Request.Query);
                            return new CachedQuery(context.Request.Query, document);
                        });
                    context.Document = context.CachedQuery.Document;
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

