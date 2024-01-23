using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Execution;

internal static class FusionContextExtensions
{
    public static ExecutionState RegisterState(
        this FusionExecutionContext context,
        SelectionSet selectionSet,
        ObjectResult result,
        SelectionData parentData = default)
    {
        var exportKeys = context.QueryPlan.GetExportKeys(selectionSet);

        var workItem = new ExecutionState(selectionSet, result, exportKeys)
        {
            SelectionSetData = { [0] = parentData, },
        };

        context.State.RegisterState(workItem);

        return workItem;
    }

    public static void TryRegisterState(
        this FusionExecutionContext context,
        SelectionSet selectionSet,
        ObjectResult result,
        SelectionData parentData = default)
    {
        var exportKeys = context.QueryPlan.GetExportKeys(selectionSet);
        context.State.TryRegisterState(selectionSet, result, exportKeys, parentData);
    }
}
