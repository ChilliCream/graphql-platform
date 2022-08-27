using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal static class FederationContextExtensions
{
    public static void RegisterState(
        this IFederationContext context,
        ISelectionSet selectionSet,
        ObjectResult result,
        SelectionResult parentResult = default)
    {
        var exportKeys = context.QueryPlan.GetExportKeys(selectionSet);

        var workItem = new WorkItem(selectionSet, result, exportKeys)
        {
            SelectionResults = { [0] = parentResult }
        };

        context.State.RegisterState(workItem);
    }
}
