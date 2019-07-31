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
        private readonly Cache<ICachedQuery> _queryCache;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;
        private readonly IDocumentHashProvider _documentHashProvider;

        public ParseQueryMiddleware(
            QueryDelegate next,
            IQueryParser parser,
            Cache<ICachedQuery> queryCache,
            QueryExecutionDiagnostics diagnosticEvents,
            IDocumentHashProvider documentHashProvider)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _parser = parser
                ?? throw new ArgumentNullException(nameof(parser));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
            _diagnosticEvents = diagnosticEvents
                ?? throw new ArgumentNullException(nameof(diagnosticEvents));
            _documentHashProvider = documentHashProvider
                ?? new MD5DocumentHashProvider();
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
                    string queryKey = context.Request.QueryName;
                    ICachedQuery cachedQuery = null;

                    if (queryKey is null || context.Request.Query != null)
                    {
                        queryKey = _documentHashProvider.ComputeHash(
                            context.Request.Query.ToSource());
                    }

                    if (context.Request.Query is null
                        && !_queryCache.TryGet(queryKey, out cachedQuery))
                    {
                        // TODO : check for query storage here?
                        // TODO : RESOURCES
                        context.Result = QueryResult.CreateError(
                            ErrorBuilder.New()
                                .SetMessage("persistedQueryNotFound")
                                .SetCode("CACHED_QUERY_NOT_FOUND")
                                .Build());
                    }

                    if (cachedQuery is null)
                    {
                        cachedQuery = _queryCache.GetOrCreate(
                            queryKey,
                            () =>
                            {
                                documentRetrievedFromCache = false;
                                DocumentNode document =
                                    ParseDocument(context.Request.Query);
                                return new CachedQuery(queryKey, document);
                            });
                    }

                    // update context
                    context.QueryKey = queryKey;
                    context.CachedQuery = cachedQuery;
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

        private DocumentNode ParseDocument(IQuery query)
        {
            if (query is QueryDocument parsed)
            {
                return parsed.Document;
            }

            if (query is QuerySourceText source)
            {
                return _parser.Parse(source.ToSource());
            }

            // TODO : resources
            throw new NotSupportedException(
                "The specified query type is not supported.");
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request == null
                || context.Request.Query == null;
        }
    }
}

