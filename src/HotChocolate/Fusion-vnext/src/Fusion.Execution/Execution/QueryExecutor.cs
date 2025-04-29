using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public class QueryExecutor
{
    public async ValueTask QueryAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var nodeMap = context.OperationPlan.AllNodes.ToDictionary(t => t.Id);
        var waitingToRun = new HashSet<ExecutionNode>(context.OperationPlan.AllNodes);
        var completed = new HashSet<ExecutionNode>();
        var running = new HashSet<Task<ExecutionStatus>>();

        foreach (var root in context.OperationPlan.RootNodes)
        {
            waitingToRun.Remove(root);
            running.Add(root.ExecuteAsync(context, cancellationToken));
        }

        while (running.Count > 0)
        {
            var status = await Task.WhenAny(running);
            running.Remove(status);
            completed.Add(nodeMap[status.Result.Id]);
            EnqueueNextNodes(context, waitingToRun, completed, running, cancellationToken);
        }

        // assemble the result
    }

    private static void EnqueueNextNodes(
        OperationPlanContext context,
        HashSet<ExecutionNode> waitingToRun,
        HashSet<ExecutionNode> completed,
        HashSet<Task<ExecutionStatus>> running,
        CancellationToken cancellationToken)
    {
        var selected = new List<ExecutionNode>();

        foreach (var node in waitingToRun)
        {
            if (completed.IsSupersetOf(node.Dependencies))
            {
                selected.Add(node);
            }
        }

        foreach (var node in selected)
        {
            waitingToRun.Remove(node);
            running.Add(node.ExecuteAsync(context, cancellationToken));
        }
    }
}
