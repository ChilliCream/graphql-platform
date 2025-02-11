namespace HotChocolate.Types;

public interface IListTypeDefinition : ITypeDefinition
{
    ITypeDefinition ElementType { get; }
}
