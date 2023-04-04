using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal static class FusionContextExtensions
{
    public static WorkItem RegisterState(
        this FusionExecutionContext context,
        ISelectionSet selectionSet,
        ObjectResult result,
        SelectionData parentData = default)
    {
        var exportKeys = context.QueryPlan.GetExportKeys(selectionSet);

        var workItem = new WorkItem(selectionSet, result, exportKeys)
        {
            SelectionSetData = { [0] = parentData }
        };

        context.State.RegisterState(workItem);

        return workItem;
    }
}
