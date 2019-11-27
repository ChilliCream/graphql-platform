namespace HotChocolate.Language
{
    public interface IExecutableDefinitionNode
        : IDefinitionNode
    {
        NameNode Name { get; }
    }
}
