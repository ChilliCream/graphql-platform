using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class WritePersistedOperationMiddleware
{
    private const string PersistedQuery = "persistedQuery";
    private const string Persisted = "persisted";
    private const string ExpectedValue = "expectedHashValue";
    private const string ExpectedType = "expectedHashType";
    private const string ExpectedFormat = "expectedHashFormat";
    private readonly RequestDelegate _next;
    private readonly IDocumentHashProvider _hashProvider;
    private readonly IOperationDocumentStorage _operationDocumentStorage;

    private WritePersistedOperationMiddleware(RequestDelegate next,
        IDocumentHashProvider documentHashProvider,
        [SchemaService] IOperationDocumentStorage operationDocumentStorage)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _hashProvider = documentHashProvider ??
            throw new ArgumentNullException(nameof(documentHashProvider));
        _operationDocumentStorage = operationDocumentStorage ??
            throw new ArgumentNullException(nameof(operationDocumentStorage));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!context.IsCachedDocument &&
            context.Document is not null &&
            context.DocumentId is { } documentId &&
            context.Request.Document is { } document &&
            context.Result is IOperationResult result &&
            context.IsValidDocument &&
            context.Request.Extensions is not null &&
            context.Request.Extensions.TryGetValue(PersistedQuery, out var s) &&
            s is IReadOnlyDictionary<string, object> settings)
        {
            var resultBuilder = OperationResultBuilder.FromResult(result);

            // hash is found and matches the query key -> store the query
            if (DoHashesMatch(settings, documentId, _hashProvider.Name, out var userHash))
            {
                // save the query
                await _operationDocumentStorage.SaveAsync(documentId, document).ConfigureAwait(false);

                // add persistence receipt to the result
                resultBuilder.SetExtension(
                    PersistedQuery,
                    new Dictionary<string, object>
                    {
                        { _hashProvider.Name, userHash },
                        { Persisted, true }
                    });

                context.ContextData[WellKnownContextData.DocumentSaved] = true;
            }
            else
            {
                resultBuilder.SetExtension(
                    PersistedQuery,
                    new Dictionary<string, object?>
                    {
                        { _hashProvider.Name, userHash },
                        { ExpectedValue, context.DocumentId },
                        { ExpectedType, _hashProvider.Name },
                        { ExpectedFormat, _hashProvider.Format.ToString() },
                        { Persisted, false }
                    });
            }

            context.Result = resultBuilder.Build();
        }
    }

    private static bool DoHashesMatch(
        IReadOnlyDictionary<string, object> settings,
        OperationDocumentId expectedHash,
        string hashName,
        [NotNullWhen(true)] out string? userHash)
    {
        if (settings.TryGetValue(hashName, out var h) &&
            h is string hash)
        {
            userHash = hash;
            return hash.Equals(expectedHash.Value, StringComparison.Ordinal);
        }

        userHash = null;
        return false;
    }

    public static RequestCoreMiddlewareConfiguration Create()
        => new RequestCoreMiddlewareConfiguration(
            (core, next) =>
            {
                var documentHashProvider = core.Services.GetRequiredService<IDocumentHashProvider>();
                var persistedOperationStore = core.SchemaServices.GetRequiredService<IOperationDocumentStorage>();
                var middleware = new WritePersistedOperationMiddleware(next, documentHashProvider, persistedOperationStore);
                return context => middleware.InvokeAsync(context);
            },
            nameof(WritePersistedOperationMiddleware));
}
