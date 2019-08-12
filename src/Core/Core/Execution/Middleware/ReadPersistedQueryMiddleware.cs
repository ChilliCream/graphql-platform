using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class ReadPersistedQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly Cache<ICachedQuery> _queryCache;
        private readonly IReadStoredQueries _readStoredQueries;
        private readonly IErrorHandler _errorHandler;

        public ReadPersistedQueryMiddleware(
            QueryDelegate next,
            Cache<ICachedQuery> queryCache,
            IReadStoredQueries readStoredQueries,
            IErrorHandler errorHandler)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
            _readStoredQueries = readStoredQueries
                ?? throw new ArgumentNullException(nameof(readStoredQueries));
            _errorHandler = errorHandler
                ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.Read_PQ_Middleware_Incomplete)
                        .SetCode(MiddlewareErrorCodes.Incomplete)
                        .Build());
                return;
            }

            if (context.Document is null)
            {
                ICachedQuery cachedQuery = null;
                IQuery query =
                    await _readStoredQueries.TryReadQueryAsync(context.QueryKey)
                        .ConfigureAwait(false);

                if (query == null)
                {
                    context.Result = QueryResult.CreateError(
                        _errorHandler.Handle(ErrorBuilder.New()
                            .SetMessage(CoreResources.Read_PQ_Middleware_QueryNotFound)
                            .SetCode(MiddlewareErrorCodes.QueryNotFound)
                            .Build()));
                    return;
                }

                cachedQuery = _queryCache.GetOrCreate(
                    context.QueryKey,
                    () =>
                    {
                        DocumentNode document =
                            ParseDocument(query);
                        return new CachedQuery(context.QueryKey, document);
                    });

                // update context
                context.CachedQuery = cachedQuery;
                context.Document = context.CachedQuery.Document;
                context.ContextData[ContextDataKeys.DocumentCached] = true;
            }

            await _next(context).ConfigureAwait(false);
        }

        private static DocumentNode ParseDocument(IQuery query)
        {
            if (query is QueryDocument parsed)
            {
                return parsed.Document;
            }

            throw new NotSupportedException(
                CoreResources.Read_PQ_Middleware_QueryTypeNotSupported);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request is null
                || (context.Request.Query is null
                    && context.Request.QueryName is null);
        }
    }
}
