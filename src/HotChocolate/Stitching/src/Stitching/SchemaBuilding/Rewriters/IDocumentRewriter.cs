using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding.Rewriters;

public interface IDocumentRewriter
{
    DocumentNode Rewrite(
        ISchemaInfo schema,
        DocumentNode document);
}
