namespace HotChocolate.Fusion.Metadata;

public sealed class ObjectType : IType
{
    public ObjectType(
        string name,
        IEnumerable<MemberBinding> bindings,
        IEnumerable<FetchDefinition> resolvers,
        IEnumerable<ObjectField> fields)
    {
        Name = name;
        Bindings = new MemberBindingCollection(bindings);
        Resolvers = new FetchDefinitionCollection(resolvers);
        Fields = new ObjectFieldCollection(fields);
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public FetchDefinitionCollection Resolvers { get; }

    public VariableDefinitionCollection Variables { get; } = new();

    public ObjectFieldCollection Fields { get; }
}
