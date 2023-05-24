using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class WritePersistedQueryMiddleware
{
    private const string PersistedQueryKey = "persistedQuery";
    private const string PersistedKey = "persisted";
    private const string ExpectedValueKey = "expectedHashValue";
    private const string ExpectedTypeKey = "expectedHashType";
    private const string ExpectedFormatKey = "expectedHashFormat";

    private readonly RequestDelegate _next;
    private readonly IDocumentHashProvider _hashProvider;
    private readonly IWriteStoredQueries _persistedQueryStore;

    public WritePersistedQueryMiddleware(
        RequestDelegate next,
        IDocumentHashProvider documentHashProvider,
        IWriteStoredQueries persistedQueryStore)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _hashProvider = documentHashProvider ??
            throw new ArgumentNullException(nameof(documentHashProvider));
        _persistedQueryStore = persistedQueryStore ??
            throw new ArgumentNullException(nameof(persistedQueryStore));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!context.IsCachedDocument &&
            context.Document is { } &&
            context.DocumentId is { } documentId &&
            context.Request.Query is { } query &&
            context.Result is IQueryResult result &&
            context.IsValidDocument &&
            context.Request.Extensions is { } &&
            context.Request.Extensions.TryGetValue(PersistedQueryKey, out var s) &&
            s is IReadOnlyDictionary<string, object> settings)
        {
            IQueryResultBuilder builder = QueryResultBuilder.FromResult(result);

            // hash is found and matches the query key -> store the query
            if (DoHashesMatch(settings, documentId, _hashProvider.Name, out var userHash))
            {
                // save the query
                await _persistedQueryStore.WriteQueryAsync(documentId, query).ConfigureAwait(false);

                // add persistence receipt to the result
                builder.SetExtension(
                    PersistedQueryKey,
                    new Dictionary<string, object>
                    {
                        { _hashProvider.Name, userHash },
                        { PersistedKey, true }
                    });

                context.ContextData[WellKnownContextData.DocumentSaved] = true;
            }
            else
            {
                builder.SetExtension(
                    PersistedQueryKey,
                    new Dictionary<string, object?>
                    {
                        { _hashProvider.Name, userHash },
                        { ExpectedValueKey, context.DocumentId },
                        { ExpectedTypeKey, _hashProvider.Name },
                        { ExpectedFormatKey, _hashProvider.Format.ToString() },
                        { PersistedKey, false }
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
        if (settings.TryGetValue(hashName, out var h) &&
            h is string hash)
        {
            userHash = hash;
            return hash.Equals(expectedHash, StringComparison.Ordinal);
        }

        userHash = null;
        return false;
    }
}
