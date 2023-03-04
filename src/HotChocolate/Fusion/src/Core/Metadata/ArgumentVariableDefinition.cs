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
        Subgraph = subgraph;
        Type = type;
        ArgumentName = argumentName;
    }

    public string Name { get; }

    public string Subgraph { get; }

    public ITypeNode Type { get; }

    public string ArgumentName { get; }
}
