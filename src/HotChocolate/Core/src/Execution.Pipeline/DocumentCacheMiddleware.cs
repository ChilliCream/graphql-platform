using System.Buffers;
using System.Text;
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

    public ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        var request = context.Request;
        var addToCache = true;

        if (documentInfo.Document is null)
        {
            if (!request.DocumentId.IsEmpty
                && _documentCache.TryGetDocument(request.DocumentId.Value, out var document))
            {
                UseCachedDocument(context, request.DocumentId, document);
                addToCache = false;
            }
            else if (!request.DocumentHash.IsEmpty
                && _documentCache.TryGetDocument(request.DocumentHash.Value, out document))
            {
                UseCachedDocument(context, new OperationDocumentId(request.DocumentHash.Value), document);
                addToCache = false;
            }
            else if (!request.DocumentHash.IsEmpty && request.Document is not null)
            {
                documentInfo.Hash = _hashProvider.ComputeHash(request.Document.AsSpan());
                if (_documentCache.TryGetDocument(documentInfo.Hash.Value, out document))
                {
                    UseCachedDocument(context, new OperationDocumentId(documentInfo.Hash.Value), document);
                    addToCache = false;
                }
            }
            else if (request.DocumentId.IsEmpty
                && request.Document is OperationDocumentSourceText sourceText)
            {
                // Source text without an id or hash is looked up by the hash of the text,
                // which is the id the document is cached under after its first validation.
                var hash = ComputeHash(sourceText.SourceText);
                if (_documentCache.TryGetDocument(hash.Value, out document))
                {
                    UseCachedDocument(context, new OperationDocumentId(hash.Value), document);
                    addToCache = false;
                }
            }
        }

        if (!addToCache)
        {
            return _next(context);
        }

        return InvokeAndAddToCacheAsync(context, documentInfo);
    }

    private void UseCachedDocument(RequestContext context, OperationDocumentId id, CachedDocument document)
    {
        var documentInfo = context.OperationDocumentInfo;
        documentInfo.Id = id;
        documentInfo.Hash = document.Hash;
        documentInfo.Document = document.Body;
        documentInfo.IsValidated = true;
        documentInfo.IsCached = true;
        documentInfo.IsPersisted = document.IsPersisted;
        _diagnosticEvents.RetrievedDocumentFromCache(context);
    }

    private OperationDocumentHash ComputeHash(string sourceText)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(sourceText.Length));

        try
        {
            var written = Encoding.UTF8.GetBytes(sourceText, buffer);
            return _hashProvider.ComputeHash(buffer.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async ValueTask InvokeAndAddToCacheAsync(RequestContext context, OperationDocumentInfo documentInfo)
    {
        await _next(context).ConfigureAwait(false);

        if (!OperationDocumentId.IsNullOrEmpty(documentInfo.Id)
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
            if (!documentInfo.Hash.IsEmpty
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
            WellKnownRequestMiddleware.DocumentCacheMiddleware);

    internal static DocumentCacheMiddleware Create(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IDocumentCache documentCache,
        IDocumentHashProvider documentHashProvider)
        => new(next, diagnosticEvents, documentCache, documentHashProvider);
}
