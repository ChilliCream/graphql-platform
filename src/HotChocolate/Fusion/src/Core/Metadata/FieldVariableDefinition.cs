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
        SubgraphName = subgraph;
        Select = select;
    }

    public string Name { get; }

    public string SubgraphName { get; }

    public FieldNode Select { get; }
}
