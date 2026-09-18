using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline;

internal sealed class OperationDocumentNormalizer : IOperationDocumentNormalizer
{
    private readonly InlineFragmentOperationRewriter _documentRewriter;

    public OperationDocumentNormalizer(ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _documentRewriter = new InlineFragmentOperationRewriter(
            schema,
            removeStaticallyExcludedSelections: true,
            includeTypeNameToEmptySelectionSets: false);
    }

    public DocumentNode NormalizeDocument(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var documentInfo = context.OperationDocumentInfo;
        var document = documentInfo.Document
            ?? throw HotChocolate.Execution.ThrowHelper.OperationDocumentNotAvailable();

        return NormalizeDocument(document, context.Request.OperationName);
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
