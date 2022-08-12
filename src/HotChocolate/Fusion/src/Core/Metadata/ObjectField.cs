namespace HotChocolate.Fusion.Metadata;

internal sealed class ObjectField
{
    public ObjectField(
        string name,
        MemberBindingCollection bindings,
        ArgumentVariableDefinitionCollection variables,
        FetchDefinitionCollection resolvers)
    {
        Name = name;
        Bindings = bindings;
        Variables =variables;
        Resolvers = resolvers;
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public ArgumentVariableDefinitionCollection Variables { get; }

    public FetchDefinitionCollection Resolvers { get; }
}
