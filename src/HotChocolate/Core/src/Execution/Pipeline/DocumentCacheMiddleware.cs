using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IDocumentCache _documentCache;
    private readonly IDocumentHashProvider _documentHashProvider;

    public DocumentCacheMiddleware(
        RequestDelegate next,
        IExecutionDiagnosticEvents diagnosticEvents,
        IDocumentCache documentCache,
        IDocumentHashProvider documentHashProvider)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _documentCache = documentCache ??
            throw new ArgumentNullException(nameof(documentCache));
        _documentHashProvider = documentHashProvider ??
            throw new ArgumentNullException(nameof(documentHashProvider));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        var request = context.Request;
        var addToCache = true;

        if (context.Document is null)
        {
            if (request.QueryId != null &&
                _documentCache.TryGetDocument(request.QueryId, out var document))
            {
                context.DocumentId = request.QueryId;
                context.Document = document;
                context.ValidationResult = DocumentValidatorResult.Ok;
                context.IsCachedDocument = true;
                addToCache = false;
                _diagnosticEvents.RetrievedDocumentFromCache(context);
            }
            else if (request.QueryHash != null &&
                _documentCache.TryGetDocument(request.QueryHash, out document))
            {
                context.DocumentId = request.QueryHash;
                context.Document = document;
                context.ValidationResult = DocumentValidatorResult.Ok;
                context.IsCachedDocument = true;
                addToCache = false;
                _diagnosticEvents.RetrievedDocumentFromCache(context);
            }
            else if (request.QueryHash is null && request.Query != null)
            {
                context.DocumentHash =
                    _documentHashProvider.ComputeHash(request.Query.AsSpan());

                if (_documentCache.TryGetDocument(context.DocumentHash, out document))
                {
                    context.DocumentId = context.DocumentHash;
                    context.Document = document;
                    context.ValidationResult = DocumentValidatorResult.Ok;
                    context.IsCachedDocument = true;
                    addToCache = false;
                    _diagnosticEvents.RetrievedDocumentFromCache(context);
                }
            }
        }

        await _next(context).ConfigureAwait(false);

        if (addToCache &&
            context.DocumentId != null &&
            context.Document != null &&
            context.IsValidDocument)
        {
            _documentCache.TryAddDocument(context.DocumentId, context.Document);
            _diagnosticEvents.AddedDocumentToCache(context);
        }
    }
}
