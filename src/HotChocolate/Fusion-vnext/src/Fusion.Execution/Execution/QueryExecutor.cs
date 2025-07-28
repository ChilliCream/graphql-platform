using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public class QueryExecutor
{
    public async ValueTask<IExecutionResult> QueryAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        var nodeMap = context.OperationPlan.AllNodes.ToDictionary(t => t.Id);
        var waitingToRun = new HashSet<ExecutionNode>(context.OperationPlan.AllNodes);
        var completed = new HashSet<ExecutionNode>();
        var running = new HashSet<Task<ExecutionNodeResult>>();

        foreach (var root in context.OperationPlan.RootNodes)
        {
            waitingToRun.Remove(root);
            running.Add(root.ExecuteAsync(context, cancellationToken));
        }

        while (running.Count > 0)
        {
            var task = await Task.WhenAny(running);
            running.Remove(task);

            var node = nodeMap[task.Result.Id];

            if (task.Result.Status is ExecutionStatus.Skipped)
            {
                // if a node is skipped, all dependents are skipped as well
                PurgeSkippedNodes(node, waitingToRun);
            }
            else
            {
                completed.Add(node);
                EnqueueNextNodes(context, waitingToRun, completed, running, cancellationToken);
            }
        }

        return context.CreateFinalResult();
    }

    private static void EnqueueNextNodes(
        OperationPlanContext context,
        HashSet<ExecutionNode> waitingToRun,
        HashSet<ExecutionNode> completed,
        HashSet<Task<ExecutionNodeResult>> running,
        CancellationToken cancellationToken)
    {
        var selected = new List<ExecutionNode>();

        foreach (var node in waitingToRun)
        {
            var isSuperset = true;

            foreach (var dependency in node.Dependencies)
            {
                if (!completed.Contains(dependency))
                {
                    isSuperset = false;
                    break;
                }
            }

            if (isSuperset)
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

    private static void PurgeSkippedNodes(ExecutionNode skipped, HashSet<ExecutionNode> waitingToRun)
    {
        var remove = new Stack<ExecutionNode>();
        remove.Push(skipped);

        while (remove.TryPop(out var node))
        {
            waitingToRun.Remove(node);

            foreach (var enqueuedNode in waitingToRun)
            {
                if (enqueuedNode.Dependencies.Contains(enqueuedNode))
                {
                    remove.Push(enqueuedNode);
                }
            }
        }
    }
}
