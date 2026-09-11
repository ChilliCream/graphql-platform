using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class DocumentNormalizationMiddleware
{
    private readonly DocumentRewriter _documentRewriter;
    private readonly IDocumentCache _documentCache;

    private DocumentNormalizationMiddleware(FusionSchemaDefinition schema, IDocumentCache documentCache)
    {
        _documentRewriter = new DocumentRewriter(schema, removeStaticallyExcludedSelections: true);
        _documentCache = documentCache;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document;

        if (document is null)
        {
            throw new InvalidOperationException(
                "The operation document info is not available in the context.");
        }

        CachedDocument? cachedDocument = null;

        if (!documentInfo.Id.IsEmpty && documentInfo.OperationCount == 1)
        {
            _documentCache.TryGetDocument(documentInfo.Id.Value, out cachedDocument);
        }

        var normalizedDocument = cachedDocument?.NormalizedBody;

        if (normalizedDocument is null)
        {
            // Before we can plan an operation, we must de-fragmentize it and remove static include conditions.
            normalizedDocument = _documentRewriter.RewriteDocument(document, context.Request.OperationName);

            // If the document is already in the document cache, we keep the normalized body on
            // the cached entry so that later hits for the same document can reuse it. Multi-operation
            // documents are rewritten per request because the normalized body is operation-specific.
            if (cachedDocument is not null)
            {
                cachedDocument.NormalizedBody = normalizedDocument;
            }
        }

        var normalizedOperation = normalizedDocument.GetOperation(context.Request.OperationName);
        context.SetNormalizedDocument(normalizedDocument, normalizedOperation);

        return next(context);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var documentCache = fc.SchemaServices.GetRequiredService<IDocumentCache>();
                var middleware = new DocumentNormalizationMiddleware((FusionSchemaDefinition)fc.Schema, documentCache);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.DocumentNormalizationMiddleware);
}
