using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

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
                        .SetMessage(CoreResources.ParseQueryMiddleware_InComplete)
                        .SetCode(MiddlewareErrorCodes.Incomplete)
                        .Build());
            }
            else
            {
                Activity activity = _diagnosticEvents.BeginParsing(context);

                try
                {
                    bool documentRetrievedFromCache = true;
                    ICachedQuery cachedQuery = null;
                    string queryKey = ResolveQueryKey(context.Request);

                    if (context.Request.Query is null)
                    {
                        _queryCache.TryGet(queryKey, out cachedQuery);
                    }
                    else
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

                    if (cachedQuery != null)
                    {
                        context.CachedQuery = cachedQuery;
                        context.Document = cachedQuery.Document;
                        context.ContextData[ContextDataKeys.DocumentCached] =
                            documentRetrievedFromCache;
                    }
                }
                finally
                {
                    _diagnosticEvents.EndParsing(activity, context);
                }

                await _next(context).ConfigureAwait(false);
            }
        }

        private string ResolveQueryKey(IReadOnlyQueryRequest request)
        {
            string queryKey = request.QueryName;

            if (queryKey is null || request.Query != null)
            {
                queryKey = request.QueryHash is null
                    ? _documentHashProvider.ComputeHash(
                        request.Query.ToSpan())
                    : request.QueryHash;
            }

            return queryKey;
        }

        private DocumentNode ParseDocument(IQuery query)
        {
            if (query is QueryDocument parsed)
            {
                return parsed.Document;
            }

            if (query is QuerySourceText source)
            {
                return _parser.Parse(source.ToSpan());
            }

            throw new NotSupportedException(
                CoreResources.ParseQuery_Middleware_QueryTypeNotSupported);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request is null
                || (context.Request.Query is null
                    && context.Request.QueryName is null);
        }
    }

    internal sealed class NoCachedQueryErrorMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IErrorHandler _errorHandler;

        public NoCachedQueryErrorMiddleware(
            QueryDelegate next,
            IErrorHandler errorHandler)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _errorHandler = errorHandler
                ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public Task InvokeAsync(IQueryContext context)
        {
            if (context.Document is null)
            {
                context.Result = QueryResult.CreateError(
                    _errorHandler.Handle(ErrorBuilder.New()
                        .SetMessage("CachedQueryNotFound")
                        .SetCode("CACHED_QUERY_NOT_FOUND")
                        .Build()));
                return Task.CompletedTask;
            }
            else
            {
                return _next.Invoke(context);
            }
        }
    }
}
