using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal static class QueryPlannerHelpers
{
    public static string? GetBestMatchingSubgraph(
        this FusionGraphConfiguration configuration,
        IOperation operation,
        SelectionPath? parentSelectionPath,
        IReadOnlyList<ISelection> selections,
        ObjectTypeMetadata typeMetadataContext,
        IReadOnlyList<string>? availableSubgraphs = null)
    {
        var bestScore = 0;
        var bestSubgraph = default(string?);

        foreach (var subgraphName in availableSubgraphs ?? configuration.SubgraphNames)
        {
            var score =
                EvaluateSubgraphCompatibilityScore(
                    configuration,
                    operation,
                    parentSelectionPath,
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
        SelectionPath? parentSelectionPath,
        IReadOnlyList<ISelection> selections,
        ObjectTypeMetadata typeMetadataContext,
        string schemaName)
    {
        var score = 0;

        var pathOrTypeCanBeResolvedFromRoot = EnsurePathOrTypeCanBeResolvedFromRoot(configuration, parentSelectionPath, typeMetadataContext, schemaName);

        var stack = new Stack<(IReadOnlyList<ISelection> selections, ObjectTypeMetadata typeContext)>();
        stack.Push((selections, typeMetadataContext));

        while (stack.Count > 0)
        {
            var (currentSelections, currentTypeContext) = stack.Pop();

            // If there are no selections at the current node, it means the subgraph
            // can resolve the path up to this point without requiring any further fields, so we increase the score.
            if (currentSelections.Count == 0)
            {
                score++;
            }

            foreach (var selection in currentSelections)
            {
                if (!selection.Field.IsIntrospectionField &&
                    currentTypeContext.Fields[selection.Field.Name].Bindings
                        .ContainsSubgraph(schemaName) &&
                    (pathOrTypeCanBeResolvedFromRoot ||
                        currentTypeContext.Fields[selection.Field.Name].Resolvers
                        .ContainsResolvers(schemaName)))
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

    private static bool EnsurePathOrTypeCanBeResolvedFromRoot(
        FusionGraphConfiguration configuration,
        SelectionPath? parentSelectionPath,
        ObjectTypeMetadata typeMetadataContext,
        string schemaName)
    {
        return typeMetadataContext.Resolvers.ContainsResolvers(schemaName) ||
            configuration.EnsurePathCanBeResolvedFromRoot(schemaName, parentSelectionPath);
    }

    public static bool EnsurePathCanBeResolvedFromRoot(
        this FusionGraphConfiguration configuration,
        string subgraphName,
        SelectionPath? path)
    {
        var current = path;

        while (current is not null)
        {
            var typeMetadata = configuration.GetType<ObjectTypeMetadata>(current.Selection.DeclaringType.Name);

            if (!typeMetadata.Fields[current.Selection.Field.Name].Bindings.ContainsSubgraph(subgraphName))
            {
                return false;
            }

            current = current.Parent;
        }

        return true;
    }
}
