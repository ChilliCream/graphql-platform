using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeDocumentDefinitionOperation : ISchemaNodeOperation<DocumentNode>
{
    public DocumentNode Apply(DocumentNode source, DocumentNode target, MergeOperationContext context)
    {
        return target;
    }
}
