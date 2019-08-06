using System;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);

        [Obsolete("Use AddTypeMergeRule")]
        ISchemaMerger AddMergeRule(MergeTypeRuleFactory factory);

        ISchemaMerger AddTypeMergeRule(MergeTypeRuleFactory factory);

        ISchemaMerger AddDirectiveMergeRule(MergeDirectiveRuleFactory factory);

        ISchemaMerger AddTypeRewriter(ITypeRewriter rewriter);

        ISchemaMerger AddDocumentRewriter(IDocumentRewriter rewriter);

        DocumentNode Merge();
    }
}
