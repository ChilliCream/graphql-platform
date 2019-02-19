using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMerger
    {
        ISchemaMerger AddSchema(NameString name, DocumentNode schema);
        ISchemaMerger AddMergeHandler(MergeTypeHandler mergeHandler);
        IStitchingBuilder IgnoreRootTypes(
            NameString schemaName);
        IStitchingContext IgnoreType(
            NameString schemaName,
            NameString typeName);
        IStitchingContext IgnoreField(
            NameString schemaName,
            FieldReference field);
        IStitchingContext RenameType(
            NameString schemaName,
            NameString typeName,
            NameString newName);
        IStitchingContext RenameField(
            NameString schemaName,
            FieldReference field,
            NameString newName);
        DocumentNode Merge();
    }
}
