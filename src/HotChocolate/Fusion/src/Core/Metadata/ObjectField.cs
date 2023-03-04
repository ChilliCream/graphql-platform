namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectField
{
    public ObjectField(
        string name,
        MemberBindingCollection bindings,
        FieldVariableDefinitionCollection variables,
        ResolverDefinitionCollection resolvers)
    {
        Name = name;
        Bindings = bindings;
        Variables = variables;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public FieldVariableDefinitionCollection Variables { get; }

    public ResolverDefinitionCollection Resolvers { get; }
}
