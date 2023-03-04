using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FieldVariableDefinition : IVariableDefinition
{
    public FieldVariableDefinition(
        string name,
        string subgraph,
        ITypeNode type,
        FieldNode select)
    {
        Name = name;
        Subgraph = subgraph;
        Type = type;
        Select = select;
    }

    public string Name { get; }

    public string Subgraph { get; }

    public ITypeNode Type { get; }

    public FieldNode Select { get; }
}
