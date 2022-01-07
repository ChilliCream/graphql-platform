using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal interface ITypeInfo
{
    NameString Name { get; }

    ITypeDefinitionNode Definition { get; }
}
