namespace HotChocolate.Language
{
    /// <summary>
    /// Represents type definition that has a name like <see cref="ObejctTypeDefinitionNode" />.
    /// </summary>
    public interface ITypeDefinitionNode
        : ITypeSystemDefinitionNode
        , INamedSyntaxNode
    {
        StringValueNode? Description { get; }
    }
}
