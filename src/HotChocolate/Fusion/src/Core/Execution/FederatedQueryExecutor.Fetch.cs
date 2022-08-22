using static HotChocolate.Fusion.Execution.ExecutorUtils;

namespace HotChocolate.Fusion.Execution;

internal sealed partial class FederatedQueryExecutor
{
    private async Task ExecuteWorkItemsAsync(IFederationContext context, CancellationToken ct)
    {
        foreach (var workItem in context.Work)
        {
            var copied = workItem;
            var selectionSet = copied.SelectionSet;
            var selectionSetInfo = context.QueryPlan.GetSelectionSetInfo(selectionSet);
            copied.ExportKeys = selectionSetInfo.ExportKeys;

            ExtractPartialResult(copied);

            foreach (var node in context.QueryPlan.GetNodes(selectionSet))
            {
                if (context.WorkByNode.TryGetValue(node, out var workItems))
                {
                    workItems.Add(copied);
                }
                else
                {
                    workItems = new List<WorkItem> { copied };
                    context.WorkByNode.Add(node, workItems);
                }
            }
        }

        context.Work.Clear();

        var tasks = new List<Task>();

        while(context.WorkByNode.Count > 0)
        {
            foreach (var node in context.QueryPlan.GetNextNodes(context.Completed))
            {
                if (context.WorkByNode.TryGetValue(node, out var workItems))
                {
                    tasks.Add(node.ExecuteAsync(context, workItems, ct));
                    context.Completed.Add(node);
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
