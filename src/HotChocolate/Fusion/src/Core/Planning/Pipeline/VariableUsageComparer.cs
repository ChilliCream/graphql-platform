using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning.Pipeline;

internal sealed class VariableUsageComparer(HashSet<string> dependsOnSubgraph) : IComparer<FieldVariableDefinition>
{
    private readonly HashSet<string> _dependsOnSubgraph = dependsOnSubgraph;

    public int Compare(FieldVariableDefinition? x, FieldVariableDefinition? y)
    {
        if (x is null)
        {
            if (y is null)
            {
                return 0;
            }

            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        if (_dependsOnSubgraph.Contains(x.SubgraphName) && !_dependsOnSubgraph.Contains(y.SubgraphName))
        {
            return -1;
        }
        else if (!_dependsOnSubgraph.Contains(x.SubgraphName) && _dependsOnSubgraph.Contains(y.SubgraphName))
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
