using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class WritePersistedQueryMiddleware
    {
        private const string _persistedQuery = "persistedQuery";
        private readonly QueryDelegate _next;
        private readonly Cache<ICachedQuery> _queryCache;
        private readonly IWriteStoredQueries _writeStoredQueries;
        private readonly IDocumentHashProvider _documentHashProvider;

        public WritePersistedQueryMiddleware(
            QueryDelegate next,
            Cache<ICachedQuery> queryCache,
            IWriteStoredQueries writeStoredQueries,
            IDocumentHashProvider documentHashProvider)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
            _writeStoredQueries = writeStoredQueries
                ?? throw new ArgumentNullException(nameof(writeStoredQueries));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                // TODO : resources
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources
                            .ParseQueryMiddleware_InComplete)
                        .Build());
                return;
            }

            if (_writeStoredQueries != null
                && context.Request.Query != null
                && context.QueryKey != null
                && DoHashesMatch(context, _documentHashProvider.Name))
            {
                await _writeStoredQueries.WriteQueryAsync(
                    context.QueryKey,
                    context.Request.Query)
                    .ConfigureAwait(false);
                context.ContextData[ContextDataKeys.DocumentSaved] = true;
            }

            await _next(context).ConfigureAwait(false);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request is null;
        }

        private static bool DoHashesMatch(
            IQueryContext context,
            string hashName)
        {
            if (context.Request.Extensions.TryGetValue(
                _persistedQuery, out var s)
                && s is IReadOnlyDictionary<string, object> settings
                && settings.TryGetValue(hashName, out object h)
                && h is string hash)
            {
                return hash.Equals(context.QueryKey, StringComparison.Ordinal);
            }
            return false;
        }
    }
}
