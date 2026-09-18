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

        // Before we can plan an operation, we must de-fragmentize it and remove static
        // include conditions. The resulting document always has the operation as its
        // only definition, at Definitions[0].
        normalizedDocument = _documentRewriter.RewriteDocument(document, context.Request.OperationName);
        _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);

        return normalizedDocument;
    }
}
