using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface IMergeSchemaContext
    {
        void AddType(ITypeDefinitionNode type);
    }
}
