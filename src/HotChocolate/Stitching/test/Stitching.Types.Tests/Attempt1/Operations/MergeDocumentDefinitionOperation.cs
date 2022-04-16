using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeDocumentDefinitionOperation : ISchemaNodeOperation<DocumentNode>
{
    public DocumentNode Apply(DocumentNode source, DocumentNode target, OperationContext context)
    {
        return target;
    }
}