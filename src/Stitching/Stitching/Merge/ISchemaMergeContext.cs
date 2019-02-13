using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaMergeContext
    {
        bool ContainsType(NameString typeName);

        void AddType(ITypeDefinitionNode type);
    }
}
