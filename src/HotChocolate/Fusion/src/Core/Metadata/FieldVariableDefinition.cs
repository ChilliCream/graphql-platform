using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FieldVariableDefinition : IVariableDefinition
{
    public FieldVariableDefinition(
        string name,
        string subgraph,
        FieldNode select)
    {
        Name = name;
        Subgraph = subgraph;
        Select = select;
    }

    public string Name { get; }

    public string Subgraph { get; }

    public FieldNode Select { get; }
}
