using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Internal;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using ExecutionThrowHelper = HotChocolate.Execution.ThrowHelper;

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
            ?? throw ExecutionThrowHelper.OperationDocumentNotAvailable();

        var operationId = context.GetOperationId();

        if (_normalizedDocumentCache.TryGet(operationId, out var normalizedDocument))
        {
            return normalizedDocument;
        }

        // Inline fragments and remove statically excluded selections before compilation.
        var rewriteResult = _documentRewriter.RewriteDocument(document, context.Request.OperationName);
        normalizedDocument = ApplyIncrementalPartsMarker(rewriteResult);
        _normalizedDocumentCache.TryAdd(operationId, normalizedDocument);

        return normalizedDocument;
    }

    /// <summary>
    /// Produces a document containing only the selected operation, with fragments inlined
    /// and statically excluded selections removed.
    /// </summary>
    public static DocumentNode NormalizeDocument(
        ISchemaDefinition schema,
        DocumentNode document,
        string? operationName)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(document);

        var documentRewriter = new InlineFragmentOperationRewriter(
            schema,
            removeStaticallyExcludedSelections: true,
            includeTypeNameToEmptySelectionSets: false);

        return ApplyIncrementalPartsMarker(documentRewriter.RewriteDocument(document, operationName));
    }

    /// <summary>
    /// Marks a rewritten operation that has deferred or streamed parts.
    /// Returns the document unchanged when it has no incremental parts.
    /// </summary>
    private static DocumentNode ApplyIncrementalPartsMarker(
        InlineFragmentOperationRewriterResult rewriteResult)
    {
        if (!rewriteResult.HasIncrementalParts)
        {
            return rewriteResult.Document;
        }

        // The rewritten document contains only the selected operation.
        var operationDefinition = (OperationDefinitionNode)rewriteResult.Document.Definitions[0];
        var directives = new List<DirectiveNode>(operationDefinition.Directives)
        {
            new(InternalDirectiveNames.HasIncrementalParts)
        };
        var markedOperationDefinition = operationDefinition.WithDirectives(directives);

        return rewriteResult.Document.WithDefinitions([markedOperationDefinition]);
    }
}
