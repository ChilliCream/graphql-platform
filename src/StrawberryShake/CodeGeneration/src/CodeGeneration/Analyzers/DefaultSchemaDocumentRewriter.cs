using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

class DefaultSchemaDocumentRewriter : ISchemaDocumentRewriter
{
    public DocumentNode RewriteSchemaForOperationModel(DocumentNode document, ISchema schema)
    {
        QueryDocumentRewriter.Rewrite(document, schema);
    }
}
