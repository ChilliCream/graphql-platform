using System;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding.Rewriters;

namespace HotChocolate.Stitching.SchemaBuilding;

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
