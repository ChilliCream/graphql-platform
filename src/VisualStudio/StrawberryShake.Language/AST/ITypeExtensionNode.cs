namespace HotChocolate.Language
{
    /// <summary>
    /// Represents type extensions that has a name like <see cref="ObjectTypeExtensionNode" />.
    /// </summary>
    public interface ITypeExtensionNode
        : ITypeSystemExtensionNode
        , INamedSyntaxNode
    {
    }
}
