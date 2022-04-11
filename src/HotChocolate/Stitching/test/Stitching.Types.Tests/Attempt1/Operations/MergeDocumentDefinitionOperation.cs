using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class MergeDocumentDefinitionOperation : ISchemaNodeOperation<DocumentNode>
{
    public DocumentNode Apply(DocumentNode source, DocumentNode target, OperationContext context)
    {
        return target;
    }
}