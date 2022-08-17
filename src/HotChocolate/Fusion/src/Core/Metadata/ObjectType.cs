namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectType : IType
{
    public ObjectType(
        string name,
        MemberBindingCollection bindings,
        VariableDefinitionCollection variables,
        FetchDefinitionCollection resolvers,
        ObjectFieldCollection fields)
    {
        Name = name;
        Bindings = bindings;
        Variables = variables;
        Resolvers = resolvers;
        Fields = fields;
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public VariableDefinitionCollection Variables { get; }

    public FetchDefinitionCollection Resolvers { get; }

    public ObjectFieldCollection Fields { get; }

    public override string ToString() => Name;
}
