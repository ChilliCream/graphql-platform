using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ITypeInfo
    {
        ITypeDefinitionNode Definition { get; }

        DocumentNode Schema { get; }

        string SchemaName { get; }
    }
}
