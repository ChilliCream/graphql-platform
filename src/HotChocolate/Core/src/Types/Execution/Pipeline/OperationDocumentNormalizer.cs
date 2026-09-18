using HotChocolate.Execution.Caching;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationDocumentNormalizer : IOperationDocumentNormalizer
{
    private readonly InlineFragmentOperationRewriter _documentRewriter;
    private readonly NormalizedDocumentCache _normalizedDocumentCache;

    public OperationDocumentNormalizer(
        ISchemaDefinition schema,
        NormalizedDocumentCache normalizedDocumentCache)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(normalizedDocumentCache);

        _documentRewriter = new InlineFragmentOperationRewriter(
            schema,
            removeStaticallyExcludedSelections: true,
            includeTypeNameToEmptySelectionSets: false);
        _normalizedDocumentCache = normalizedDocumentCache;
    }

    public DocumentNode NormalizeDocument(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document
            ?? throw HotChocolate.Execution.ThrowHelper.OperationDocumentNotAvailable();

        if (!context.TryGetOperationId(out var operationId))
        {
            throw HotChocolate.Execution.ThrowHelper.OperationIdNotAvailable();
        }

        if (_normalizedDocumentCache.TryGet(operationId, out var normalizedDocument))
        {
            return normalizedDocument;
        }

        // Before we can plan an operation, we must de-fragmentize it and remove static
        // include conditions. The resulting document always has the operation as its
        // only definition, at Definitions[0].
        normalizedDocument = _documentRewriter.RewriteDocument(document, context.Request.OperationName).Document;
        _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);

        return normalizedDocument;
    }

    /// <summary>
    /// Normalizes a document directly, for callers that do not have a
    /// <see cref="RequestContext"/> at hand, such as the
    /// <see cref="HotChocolate.Execution.Processing.OperationCompiler"/> convenience overloads.
    /// </summary>
    public DocumentNode NormalizeDocument(DocumentNode document, string? operationName)
    {
        ArgumentNullException.ThrowIfNull(document);

        return _documentRewriter.RewriteDocument(document, operationName).Document;
    }
}
