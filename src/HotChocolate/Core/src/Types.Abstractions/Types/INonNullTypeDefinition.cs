namespace HotChocolate.Types;

public interface INonNullTypeDefinition : ITypeDefinition
{
    ITypeDefinition NullableType { get; }
}
