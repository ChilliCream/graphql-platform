namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectField
{
    public ObjectField(
        string name,
        MemberBindingCollection bindings,
        VariableDefinitionCollection variables,
        ResolverDefinitionCollection resolvers)
    {
        Name = name;
        Bindings = bindings;
        Variables =variables;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public VariableDefinitionCollection Variables { get; }

    public ResolverDefinitionCollection Resolvers { get; }
}
