using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal sealed class CostSchemaDocumentFormatter : ISchemaDocumentFormatter
{
    public DocumentNode Format(ISchemaDefinition schema, DocumentNode schemaDocument)
    {
        var rewriter = new CostSyntaxRewriter();
        return (DocumentNode)rewriter.Rewrite(schemaDocument, new CostSyntaxRewriter.Context(schema))!;
    }
}
