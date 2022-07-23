namespace HotChocolate.Fusion.Metadata;

public sealed class ObjectType : IType
{
    public ObjectType(
        string name,
        FetchDefinitionCollection resolvers,
        ObjectFieldCollection fields)
    {
        Name = name;
        Resolvers = new FetchDefinitionCollection(resolvers);
        Fields = new ObjectFieldCollection(fields);
    }

    public string Name { get; }

    public FetchDefinitionCollection Resolvers { get; }

    public VariableDefinitionCollection Variables { get; } = new();

    public ObjectFieldCollection Fields { get; }
}
