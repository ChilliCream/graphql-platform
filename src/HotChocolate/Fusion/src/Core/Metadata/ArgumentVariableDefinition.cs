using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ArgumentVariableDefinition : IVariableDefinition
{
    public ArgumentVariableDefinition(
        string name,
        string subgraph,
        ITypeNode type,
        string argumentName)
    {
        Name = name;
        SubgraphName = subgraph;
        Type = type;
        ArgumentName = argumentName;
    }

    public string Name { get; }

    public string SubgraphName { get; }

    public ITypeNode Type { get; }

    public string ArgumentName { get; }
}
