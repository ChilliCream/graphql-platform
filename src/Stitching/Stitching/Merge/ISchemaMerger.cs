using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);
        ISchemaMerger AddMergeHandler(MergeTypeHandler mergeHandler);
        ISchemaMerger IgnoreRootTypes(NameString schemaName);
        ISchemaMerger IgnoreType(NameString schemaName, NameString typeName);
        ISchemaMerger IgnoreField(NameString schemaName, FieldReference field);
        ISchemaMerger RenameType(NameString schemaName,
            NameString typeName, NameString newName);
        ISchemaMerger RenameField(NameString schemaName,
            FieldReference field, NameString newName);
        DocumentNode Merge();
    }
}
