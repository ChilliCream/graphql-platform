using HotChocolate.Language;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);
        ISchemaMerger AddMergeHandler(MergeTypeHandler mergeHandler);
        ISchemaMerger AddTypeRewriter(ITypeRewriter rewriter);
        ISchemaMerger AddDocumentRewriter(IDocumentRewriter rewriter);
        DocumentNode Merge();
    }
}
