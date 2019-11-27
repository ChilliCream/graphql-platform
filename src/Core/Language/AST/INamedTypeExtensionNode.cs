namespace HotChocolate.Language
{
    public interface INamedTypeExtensionNode
        : ITypeExtensionNode
    {
        NameNode Name { get; }
    }
}
