using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(string name, DocumentNode schema);
        ISchemaMerger AddHandler(MergeTypeHandler handler);
        DocumentNode Merge();
    }
}
