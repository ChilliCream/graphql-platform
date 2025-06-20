using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDocumentCache _documentCache;
    private readonly IDocumentHashProvider _hashProvider;

    private DocumentCacheMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IDocumentCache documentCache,
        IDocumentHashProvider documentHashProvider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(documentCache);
        ArgumentNullException.ThrowIfNull(documentHashProvider);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _documentCache = documentCache;
        _hashProvider = documentHashProvider;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        var request = context.Request;
        var addToCache = true;

        if (documentInfo.Document is null)
        {
            if (!request.DocumentId.IsEmpty
                && _documentCache.TryGetDocument(request.DocumentId.Value, out var document))
            {
                documentInfo.Id = request.DocumentId;
                documentInfo.Hash = document.Hash;
                documentInfo.Document = document.Body;
                documentInfo.IsValidated = true;
                documentInfo.IsCached = true;
                documentInfo.IsPersisted = document.IsPersisted;
                addToCache = false;
                _diagnosticEvents.RetrievedDocumentFromCache(context);
            }
            else if (!request.DocumentHash.IsEmpty
                && _documentCache.TryGetDocument(request.DocumentHash.Value, out document))
            {
                documentInfo.Id = new OperationDocumentId(request.DocumentHash.Value);
                documentInfo.Hash = document.Hash;
                documentInfo.Document = document.Body;
                documentInfo.IsValidated = true;
                documentInfo.IsCached = true;
                documentInfo.IsPersisted = document.IsPersisted;
                addToCache = false;
                _diagnosticEvents.RetrievedDocumentFromCache(context);
            }
            else if (!request.DocumentHash.IsEmpty && request.Document is not null)
            {
                documentInfo.Hash = _hashProvider.ComputeHash(request.Document.AsSpan());
                if (_documentCache.TryGetDocument(documentInfo.Hash.Value, out document))
                {
                    documentInfo.Id = new OperationDocumentId(documentInfo.Hash.Value);
                    documentInfo.Hash = document.Hash;
                    documentInfo.Document = document.Body;
                    documentInfo.IsValidated = true;
                    documentInfo.IsCached = true;
                    documentInfo.IsPersisted = document.IsPersisted;
                    addToCache = false;
                    _diagnosticEvents.RetrievedDocumentFromCache(context);
                }
            }
        }

        await _next(context).ConfigureAwait(false);

        if (addToCache
            && !OperationDocumentId.IsNullOrEmpty(documentInfo.Id)
            && documentInfo.Document != null
            && documentInfo.IsValidated)
        {
            _documentCache.TryAddDocument(
                documentInfo.Id.Value,
                new CachedDocument(
                    documentInfo.Document,
                    documentInfo.Hash,
                    documentInfo.IsPersisted));

            // The hash and the documentId can differ if the id is not a hash or
            // if the hash algorithm is different from the one that Hot Chocolate uses internally.
            // In case they differ, we just add another lookup to the cache.
            if(!documentInfo.Hash.IsEmpty
                && !documentInfo.Id.Value.Equals(documentInfo.Hash.Value, StringComparison.Ordinal))
            {
                _documentCache.TryAddDocument(
                    documentInfo.Hash.Value,
                    new CachedDocument(documentInfo.Document, documentInfo.Hash, documentInfo.IsPersisted));
            }

            _diagnosticEvents.AddedDocumentToCache(context);
        }
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (factoryContext, next) =>
            {
                var diagnosticEvents = factoryContext.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var documentCache = factoryContext.SchemaServices.GetRequiredService<IDocumentCache>();
                var documentHashProvider = factoryContext.SchemaServices.GetRequiredService<IDocumentHashProvider>();
                var middleware = Create(next, diagnosticEvents, documentCache, documentHashProvider);
                return context => middleware.InvokeAsync(context);
            },
            nameof(DocumentCacheMiddleware));

    internal static DocumentCacheMiddleware Create(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IDocumentCache documentCache,
        IDocumentHashProvider documentHashProvider)
        => new(next, diagnosticEvents, documentCache, documentHashProvider);
}
