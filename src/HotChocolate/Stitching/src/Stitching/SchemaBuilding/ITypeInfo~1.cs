using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

public interface ITypeInfo<out T>
    : ITypeInfo
    where T : ITypeDefinitionNode
{
    new T Definition { get; }
}
