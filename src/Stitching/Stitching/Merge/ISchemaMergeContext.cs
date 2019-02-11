using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaMergeContext
    {
        void AddType(ITypeDefinitionNode type);
    }
}
