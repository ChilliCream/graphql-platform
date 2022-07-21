namespace HotChocolate.Fusion.Types;

public sealed class ObjectField
{
    public ObjectField(
        string name,
        IEnumerable<MemberBinding> bindings,
        IEnumerable<ArgumentVariableDefinition> variables,
        IEnumerable<FetchDefinition> resolvers)
    {
        Name = name;
        Bindings = new MemberBindingCollection(bindings);
        Variables = new ArgumentVariableDefinitionCollection(variables);
        Resolvers = new FetchDefinitionCollection(resolvers);
    }

    public string Name { get; }

    public MemberBindingCollection Bindings { get; }

    public ArgumentVariableDefinitionCollection Variables { get; }

    public FetchDefinitionCollection Resolvers { get; }
}
