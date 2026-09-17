using HotChocolate.Execution.Caching;
using HotChocolate.Fusion.Rewriters;

namespace HotChocolate.Execution.Pipeline;

internal sealed class DocumentNormalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly InlineFragmentOperationRewriter _documentRewriter;
    private readonly NormalizedDocumentCache _normalizedDocumentCache;

    private DocumentNormalizationMiddleware(
        RequestDelegate next,
        ISchemaDefinition schema,
        NormalizedDocumentCache normalizedDocumentCache)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(normalizedDocumentCache);

        _next = next;
        _documentRewriter = new InlineFragmentOperationRewriter(
            schema,
            removeStaticallyExcludedSelections: true,
            includeTypeNameToEmptySelectionSets: false);
        _normalizedDocumentCache = normalizedDocumentCache;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document;

        if (document is null)
        {
            throw ThrowHelper.OperationDocumentNotAvailable();
        }

        if (!context.TryGetOperationId(out var operationId))
        {
            operationId = context.CreateCacheId();
            context.SetOperationId(operationId);
        }

        if (!_normalizedDocumentCache.TryGet(operationId, out var normalizedDocument))
        {
            normalizedDocument = _documentRewriter.RewriteDocument(
                document,
                context.Request.OperationName).Document;

            _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);
        }

        documentInfo.NormalizedDocument = normalizedDocument;

        await _next(context).ConfigureAwait(false);
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var normalizedDocumentCache = core.SchemaServices.GetRequiredService<NormalizedDocumentCache>();
                var middleware = new DocumentNormalizationMiddleware(next, core.Schema, normalizedDocumentCache);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.DocumentNormalizationMiddleware);
}
