using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Fusion.Execution.Caching;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationDocumentNormalizer : IOperationDocumentNormalizer
{
    private readonly DocumentRewriter _documentRewriter;
    private readonly NormalizedDocumentCache _normalizedDocumentCache;

    public OperationDocumentNormalizer(
        FusionSchemaDefinition schema,
        NormalizedDocumentCache normalizedDocumentCache)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(normalizedDocumentCache);

        _documentRewriter = new DocumentRewriter(schema, removeStaticallyExcludedSelections: true);
        _normalizedDocumentCache = normalizedDocumentCache;
    }

    public DocumentNode NormalizeDocument(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document
            ?? throw ThrowHelper.OperationDocumentNotAvailable();

        var operationId = context.GetOperationId();

        if (_normalizedDocumentCache.TryGet(operationId, out var normalizedDocument))
        {
            return normalizedDocument;
        }

        // Inline fragments and remove statically excluded selections before planning.
        normalizedDocument = _documentRewriter.RewriteDocument(document, context.Request.OperationName);
        _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);

        return normalizedDocument;
    }
}
