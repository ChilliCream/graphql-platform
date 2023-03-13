namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectType : IType
{
    public ObjectType(
        string name,
        MemberBindingCollection bindings,
        VariableDefinitionCollection variables,
        ResolverDefinitionCollection resolvers,
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

    public ResolverDefinitionCollection Resolvers { get; }

    public ObjectFieldCollection Fields { get; }

    public override string ToString() => Name;
}
