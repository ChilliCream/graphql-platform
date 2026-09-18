using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class DocumentNormalizationMiddleware
{
    private readonly DocumentRewriter _documentRewriter;
    private readonly NormalizedDocumentCache _normalizedDocumentCache;

    private DocumentNormalizationMiddleware(
        FusionSchemaDefinition schema,
        NormalizedDocumentCache normalizedDocumentCache)
    {
        _documentRewriter = new DocumentRewriter(schema, removeStaticallyExcludedSelections: true);
        _normalizedDocumentCache = normalizedDocumentCache;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document;

        if (document is null)
        {
            throw ThrowHelper.OperationDocumentNotAvailable();
        }

        if (documentInfo.Hash.IsEmpty)
        {
            context.Result = ErrorHelper.StateInvalidForOperationPlanCache();
            return default;
        }

        var operationId = documentInfo.OperationCount == 1
            ? documentInfo.Hash.Value
            : $"{documentInfo.Hash.Value}.{context.Request.OperationName ?? "Default"}";
        context.SetOperationId(operationId);

        if (!_normalizedDocumentCache.TryGet(operationId, out var normalizedDocument))
        {
            // Before we can plan an operation, we must de-fragmentize it and remove static
            // include conditions. The resulting document always has the operation as its
            // only definition, at Definitions[0].
            normalizedDocument = _documentRewriter.RewriteDocument(document, context.Request.OperationName);

            _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);
        }

        documentInfo.NormalizedDocument = normalizedDocument;

        return next(context);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var normalizedDocumentCache = fc.SchemaServices.GetRequiredService<NormalizedDocumentCache>();
                var middleware = new DocumentNormalizationMiddleware(
                    (FusionSchemaDefinition)fc.Schema,
                    normalizedDocumentCache);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.DocumentNormalizationMiddleware);
}
