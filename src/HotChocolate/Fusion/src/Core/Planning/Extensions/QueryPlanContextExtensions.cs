using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal static class QueryPlanContextExtensions
{
    public static HashSet<SelectionExecutionStep> GetSiblingExecutionSteps(
        this QueryPlanContext context,
        ISelectionSet selectionSet)
    {
        var executionSteps = new HashSet<SelectionExecutionStep>();

        if (context.TryGetExecutionStep(selectionSet, out var executionStep))
        {
            executionSteps.Add(executionStep);
        }

        foreach (var sibling in selectionSet.Selections)
        {
            if (context.TryGetExecutionStep(sibling, out executionStep))
            {
                executionSteps.Add(executionStep);
            }
        }

        return executionSteps;
    }
}
