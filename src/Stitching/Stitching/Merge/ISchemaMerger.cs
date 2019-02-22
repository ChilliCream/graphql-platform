using HotChocolate.Language;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);
        ISchemaMerger AddMergeRule(MergeTypeRuleFactory factory);
        ISchemaMerger AddTypeRewriter(ITypeRewriter rewriter);
        ISchemaMerger AddDocumentRewriter(IDocumentRewriter rewriter);
        DocumentNode Merge();
    }
}
