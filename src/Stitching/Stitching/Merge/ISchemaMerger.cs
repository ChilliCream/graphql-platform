using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);
        ISchemaMerger AddMergeHandler(MergeTypeHandler mergeHandler);
        DocumentNode Merge();
    }
}
