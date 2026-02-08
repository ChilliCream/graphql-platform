using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
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
        IOperationDocumentStorage operationDocumentStorage)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(documentHashProvider);
        ArgumentNullException.ThrowIfNull(operationDocumentStorage);

        _next = next;
        _hashProvider = documentHashProvider;
        _operationDocumentStorage = operationDocumentStorage;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        var documentInfo = context.OperationDocumentInfo;

        if (!documentInfo.IsCached
            && documentInfo.IsValidated
            && documentInfo.Document is not null
            && !documentInfo.Id.IsEmpty
            && context.Result is OperationResult result
            && context.Request.Document is { } document
            && context.Request.Extensions is not null
            && context.Request.Extensions.Document.RootElement.TryGetProperty(PersistedQuery, out var settings)
            && settings.ValueKind is JsonValueKind.Object)
        {
            var extensions = result.Extensions;

            // hash is found and matches the query key -> store the query
            if (DoHashesMatch(settings, documentInfo.Id, _hashProvider.Name, out var userHash))
            {
                // save the query
                await _operationDocumentStorage.SaveAsync(documentInfo.Id, document).ConfigureAwait(false);

                // add persistence receipt to the result
                extensions = extensions.SetItem(
                    PersistedQuery,
                    new Dictionary<string, object>
                    {
                        { _hashProvider.Name, userHash },
                        { Persisted, true }
                    });

                context.ContextData[ExecutionContextData.DocumentSaved] = true;
            }
            else
            {
                extensions = extensions.SetItem(
                    PersistedQuery,
                    new Dictionary<string, object?>
                    {
                        { _hashProvider.Name, userHash },
                        { ExpectedValue, documentInfo.Id.Value },
                        { ExpectedType, _hashProvider.Name },
                        { ExpectedFormat, _hashProvider.Format.ToString() },
                        { Persisted, false }
                    });
            }

            result.Extensions = extensions;
        }
    }

    private static bool DoHashesMatch(
        JsonElement settings,
        OperationDocumentId expectedHash,
        string hashName,
        [NotNullWhen(true)] out string? userHash)
    {
        if (settings.TryGetProperty(hashName, out var hash)
            && hash.ValueKind is JsonValueKind.String)
        {
            userHash = hash.GetString()!;
            return userHash.Equals(expectedHash.Value, StringComparison.Ordinal);
        }

        userHash = null;
        return false;
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var documentHashProvider = core.SchemaServices.GetRequiredService<IDocumentHashProvider>();
                var persistedOperationStore = core.SchemaServices.GetRequiredService<IOperationDocumentStorage>();
                var middleware = new WritePersistedOperationMiddleware(next, documentHashProvider, persistedOperationStore);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.WritePersistedOperationMiddleware);
}
