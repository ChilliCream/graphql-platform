using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface ISchemaMergeContext
    {
        bool ContainsType(NameString typeName);

        void AddType(ITypeDefinitionNode type);
    }
}
