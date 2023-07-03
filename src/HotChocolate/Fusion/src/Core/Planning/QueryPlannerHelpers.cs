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
        ObjectTypeInfo typeInfoContext,
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
                    typeInfoContext,
                    subgraphName);

            if (score > bestScore)
            {
                bestScore = score;
                bestSubgraph = subgraphName;
            }
        }

        return bestSubgraph;
    }

    // Returns the count of matching fields in the subgraph (?)
    private static int EvaluateSubgraphCompatibilityScore(
        FusionGraphConfiguration configuration,
        IOperation operation,
        IReadOnlyList<ISelection> selections,
        ObjectTypeInfo typeInfoContext,
        string schemaName)
    {
        var score = 0;
        var stack = new Stack<(IReadOnlyList<ISelection> selections, ObjectTypeInfo typeContext)>();
        stack.Push((selections, typeInfoContext));

        while (stack.Count > 0)
        {
            var (currentSelections, currentTypeContext) = stack.Pop();

            foreach (var selection in currentSelections)
            {
                var field = selection.Field;
                if (field.IsIntrospectionField ||
                    !currentTypeContext.Fields[field.Name].Bindings
                        .ContainsSubgraph(schemaName))
                {
                    continue;
                }

                score++;

                if (selection.SelectionSet is null)
                {
                    continue;
                }

                foreach (var possibleType in operation.GetPossibleTypes(selection))
                {
                    var type = configuration.GetType<ObjectTypeInfo>(possibleType.Name);
                    var selectionSet = operation.GetSelectionSet(selection, possibleType);
                    stack.Push((selectionSet.Selections, type));
                }
            }
        }

        return score;
    }
}
