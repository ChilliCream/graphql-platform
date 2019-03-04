namespace HotChocolate.Language
{
    public interface ITypeDefinitionNode
        : ITypeSystemDefinitionNode
        , INamedSyntaxNode
    {
        StringValueNode Description { get; }
    }
}
