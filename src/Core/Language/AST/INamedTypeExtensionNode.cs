namespace HotChocolate.Language
{
    /// <summary>
    /// Represents type extensions that have a name like <see cref="ObjectTypeExtensionNode" />.
    /// </summary>
    public interface INamedTypeExtensionNode
        : ITypeExtensionNode
        , INamedSyntaxNode
    {
    }
}
