namespace HotChocolate.Fusion.Types;

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

    public VariableDefinitionCollection Variables => throw new NotImplementedException();

    public ObjectFieldCollection Fields { get; }
}
