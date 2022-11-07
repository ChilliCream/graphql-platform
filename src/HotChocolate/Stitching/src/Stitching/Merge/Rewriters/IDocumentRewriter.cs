using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    public interface IDocumentRewriter
    {
        DocumentNode Rewrite(
            ISchemaInfo schema,
            DocumentNode document);
    }
}
