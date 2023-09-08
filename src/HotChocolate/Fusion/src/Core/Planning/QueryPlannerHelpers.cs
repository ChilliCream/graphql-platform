using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Planning;

internal static class QueryPlannerHelpers
{
    public static string GetBestMatchingSubgraph(
        this FusionGraphConfiguration configuration,
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectTypeMetadata typeMetadataContext,
        IReadOnlyList<string>? availableSubgraphs = null)
    {
        var bestScore = 0;
        var bestSubgraph = configuration.SubgraphNames[0];

        foreach (var subgraphName in availableSubgraphs ?? configuration.SubgraphNames)
        {
            var score =
                EvaluateSubgraphCompatibilityScore(
                    configuration,
                    operation,
                    selections,
                    typeMetadataContext,
                    subgraphName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSubgraph = subgraphName;
            }
        }

        return bestSubgraph;
    }

    private static int EvaluateSubgraphCompatibilityScore(
        FusionGraphConfiguration configuration,
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectTypeMetadata typeMetadataContext,
        string schemaName)
    {
        var score = 0;
        var stack = new Stack<(IReadOnlyList<ISelection> selections, ObjectTypeMetadata typeContext)>();
        stack.Push((selections, typeMetadataContext));

        while (stack.Count > 0)
        {
            var (currentSelections, currentTypeContext) = stack.Pop();

            foreach (var selection in currentSelections)
            {
                if (!selection.Field.IsIntrospectionField &&
                    currentTypeContext.Fields[selection.Field.Name].Bindings
                        .ContainsSubgraph(schemaName))
                {
                    score++;

                    if (selection.SelectionSet is not null)
                    {
                        foreach (var possibleType in operation.GetPossibleTypes(selection))
                        {
                            var type = configuration.GetType<ObjectTypeMetadata>(possibleType.Name);
                            var selectionSet = operation.GetSelectionSet(selection, possibleType);
                            stack.Push((selectionSet.Selections, type));
                        }
                    }
                }
            }
        }

        return score;
    }
}
