namespace StrawberryShake.CodeGeneration.Analyzers;

public interface ISchemaDocumentRewriter
{
    DocumentNode RewriteSchemaForOperationModel(DocumentNode document, ISchema schema);
}
