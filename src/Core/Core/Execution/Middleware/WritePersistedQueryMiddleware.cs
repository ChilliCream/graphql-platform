using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    internal sealed class WritePersistedQueryMiddleware
    {
        private const string _persistedQuery = "persistedQuery";
        private const string _persisted = "persisted";
        private readonly QueryDelegate _next;
        private readonly IWriteStoredQueries _writeStoredQueries;
        private readonly string _hashName;

        public WritePersistedQueryMiddleware(
            QueryDelegate next,
            IWriteStoredQueries writeStoredQueries,
            IDocumentHashProvider documentHashProvider)
        {
            if (documentHashProvider is null)
            {
                throw new ArgumentNullException(nameof(documentHashProvider));
            }

            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _writeStoredQueries = writeStoredQueries
                ?? throw new ArgumentNullException(nameof(writeStoredQueries));
            _hashName = documentHashProvider.Name;
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.Write_PQ_Middleware_Incomplete)
                        .SetCode(MiddlewareErrorCodes.Incomplete)                        .Build());
                return;
            }

            if (_writeStoredQueries != null
                && context.Request.Query != null
                && context.QueryKey != null
                && DoHashesMatch(context, _hashName))
            {
                // save the  query
                await _writeStoredQueries.WriteQueryAsync(
                    context.QueryKey,
                    context.Request.Query)
                    .ConfigureAwait(false);

                // add persistence receipt to the result
                if (context.Result is QueryResult result
                    && context.Request.Extensions.TryGetValue(
                        _persistedQuery, out var s)
                    && s is IReadOnlyDictionary<string, object> settings
                    && settings.TryGetValue(_hashName, out object h)
                    && h is string hash)
                {
                    result.Extensions[_persistedQuery] =
                        new Dictionary<string, object>
                        {
                            { _hashName, hash },
                            { _persisted, true }
                        };
                }

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
