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
        private const string _expectedValue = "expectedHashValue";
        private const string _expectedType = "expectedHashType";
        private const string _expectedFormat = "expectedHashFormat";
        private const string _hash = "hash";
        private readonly QueryDelegate _next;
        private readonly IWriteStoredQueries _writeStoredQueries;
        private readonly string _hashName;
        private readonly HashFormat _hashFormat;

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
            _hashFormat = documentHashProvider.Format;
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.Write_PQ_Middleware_Incomplete)
                        .SetCode(ErrorCodes.Execution.Incomplete).Build());
                return;
            }

            if (_writeStoredQueries != null
                && context.Request.Query != null
                && context.QueryKey != null
                && context.Result is QueryResult result
                && context.Request.Extensions != null
                && context.Request.Extensions.TryGetValue(_persistedQuery, out var s)
                && s is IReadOnlyDictionary<string, object> settings)
            {
                // hash is found and matches the query key -> store the query
                if (DoHashesMatch(settings, context.QueryKey, _hashName, out string userHash))
                {
                    // save the  query
                    await _writeStoredQueries.WriteQueryAsync(
                        context.QueryKey,
                        context.Request.Query)
                        .ConfigureAwait(false);

                    // add persistence receipt to the result
                    result.Extensions[_persistedQuery] =
                        new Dictionary<string, object>
                        {
                                { _hashName, userHash },
                                { _persisted, true }
                        };

                    context.ContextData[ContextDataKeys.DocumentSaved] = true;
                }
                else
                {
                    result.Extensions[_persistedQuery] =
                        new Dictionary<string, object>
                        {
                            { _hashName, userHash },
                            { _expectedValue, context.QueryKey },
                            { _expectedType, _hashName },
                            { _expectedFormat, _hashFormat.ToString() },
                            { _persisted, false }
                        };
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request is null;
        }

        private static bool DoHashesMatch(
            IReadOnlyDictionary<string, object> settings,
            string expectedHash,
            string hashName,
            out string userHash)
        {
            if (settings.TryGetValue(hashName, out object h)
                && h is string hash)
            {
                userHash = hash;
                return hash.Equals(expectedHash, StringComparison.Ordinal);
            }

            userHash = null;
            return false;
        }
    }
}
