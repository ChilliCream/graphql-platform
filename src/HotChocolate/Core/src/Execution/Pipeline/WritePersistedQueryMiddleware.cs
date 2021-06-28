using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class WritePersistedQueryMiddleware
    {
        private const string _persistedQuery = "persistedQuery";
        private const string _persisted = "persisted";
        private const string _expectedValue = "expectedHashValue";
        private const string _expectedType = "expectedHashType";
        private const string _expectedFormat = "expectedHashFormat";
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IDocumentHashProvider _hashProvider;
        private readonly IWriteStoredQueries _persistedQueryStore;

        public WritePersistedQueryMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IDocumentHashProvider documentHashProvider,
            IWriteStoredQueries persistedQueryStore)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _hashProvider = documentHashProvider ??
                throw new ArgumentNullException(nameof(documentHashProvider));
            _persistedQueryStore = persistedQueryStore ??
                throw new ArgumentNullException(nameof(persistedQueryStore));
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            await _next(context).ConfigureAwait(false);

            if (!context.IsCachedDocument &&
                context.Document is { } document &&
                context.DocumentId is { } documentId &&
                context.Request.Query is { } query &&
                context.Result is IReadOnlyQueryResult result &&
                context.ValidationResult is { HasErrors: false } &&
                context.Request.Extensions is { } &&
                context.Request.Extensions.TryGetValue(_persistedQuery, out var s) &&
                s is IReadOnlyDictionary<string, object> settings)
            {
                IQueryResultBuilder builder = QueryResultBuilder.FromResult(result);

                // hash is found and matches the query key -> store the query
                if (DoHashesMatch(settings, documentId, _hashProvider.Name, out string? userHash))
                {
                    // save the query
                    await _persistedQueryStore.WriteQueryAsync(documentId, query)
                        .ConfigureAwait(false);

                    // add persistence receipt to the result
                    builder.SetExtension(
                        _persistedQuery,
                        new Dictionary<string, object>
                        {
                            { _hashProvider.Name, userHash },
                            { _persisted, true }
                        });

                    context.ContextData[WellKnownContextData.DocumentSaved] = true;
                }
                else
                {
                    builder.SetExtension(
                        _persistedQuery,
                        new Dictionary<string, object?>
                        {
                            { _hashProvider.Name, userHash },
                            { _expectedValue, context.DocumentId },
                            { _expectedType, _hashProvider.Name },
                            { _expectedFormat, _hashProvider.Format.ToString() },
                            { _persisted, false }
                        });
                }

                context.Result = builder.Create();
            }
        }

        private static bool DoHashesMatch(
            IReadOnlyDictionary<string, object> settings,
            string? expectedHash,
            string hashName,
            [NotNullWhen(true)] out string? userHash)
        {
            if (settings.TryGetValue(hashName, out object? h) &&
                h is string hash)
            {
                userHash = hash;
                return hash.Equals(expectedHash, StringComparison.Ordinal);
            }

            userHash = null;
            return false;
        }
    }
}
