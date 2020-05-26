using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class DocumentCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;

        public DocumentCacheMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
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
            IReadOnlyQueryRequest request = context.Request;
            bool addToCache = true;

            if (request.QueryId is { } queryId &&
                _documentCache.TryGetDocument(queryId, out DocumentNode document))
            {
                context.DocumentId = queryId;
                context.Document = document;
                context.ValidationResult = DocumentValidatorResult.Ok;
                addToCache = false;
                // _diagnosticEvents.RetrievedDocumentFromCache(context);
            }
            else if (request.QueryHash is { } queryHash &&
                _documentCache.TryGetDocument(queryHash, out document))
            {
                context.DocumentId = queryHash;
                context.Document = document;
                context.ValidationResult = DocumentValidatorResult.Ok;
                addToCache = false;
                // _diagnosticEvents.RetrievedDocumentFromCache(context);
            }

            await _next(context).ConfigureAwait(false);

            if (addToCache &&
                context.DocumentId is { } &&
                context.Document is { } &&
                context.ValidationResult is { HasErrors: false })
            {
                _documentCache.TryAddDocument(context.DocumentId, context.Document);
                // _diagnosticEvents.AddedDocumentToCache(context);
            }
        }
    }
}
