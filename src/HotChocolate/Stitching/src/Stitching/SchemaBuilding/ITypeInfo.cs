using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface ITypeInfo
{
    ITypeDefinitionNode Definition { get; }

    ISchemaInfo Schema { get; }

    bool IsRootType { get; }
}
