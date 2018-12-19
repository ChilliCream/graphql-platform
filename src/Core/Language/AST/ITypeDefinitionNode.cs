namespace HotChocolate.Language
{
    public interface ITypeDefinitionNode
        : ITypeSystemDefinitionNode
        , IHasDirectives
    {
        NameNode Name { get; }

        StringValueNode Description { get; }
    }
}
