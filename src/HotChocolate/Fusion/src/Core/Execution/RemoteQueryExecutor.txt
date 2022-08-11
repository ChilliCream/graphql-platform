using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Execution;

internal sealed class RemoteQueryExecutor
{
    private readonly object _sync = new();
    private readonly RemoteRequestExecutorFactory _executorFactory;
    private readonly List<ExecutionNode> _completed = new();
    private readonly List<Response> _responses = new();
    private int _completedCount;

    public RemoteQueryExecutor(RemoteRequestExecutorFactory executorFactory)
    {
        _executorFactory = executorFactory;
    }

    public async Task ExecuteAsync(/*OperationContext context,*/ QueryPlan plan, IExecutionState state)
    {
        using var semaphore = new SemaphoreSlim(0);
        var backlog = new HashSet<ExecutionNode>(plan.ExecutionNodes);
        // var ct = context.RequestAborted;
        var ct = new CancellationToken();

        // var data = context.Result.RentObject(context.Operation.RootSelectionSet.Selections.Count);

        // enqueue root tasks
        foreach (var root in plan.RootExecutionNodes)
        {
            if (root is RequestNode requestNode)
            {
                BeginExecuteNode(requestNode, state, semaphore, ct);
                backlog.Remove(requestNode);
            }
        }

        await semaphore.WaitAsync(ct);

        if (backlog.Count > 0)
        {
            // process dependant tasks
            var backlogCopy = new List<ExecutionNode>();
            var completedCopy = new HashSet<ExecutionNode>();
            var completedIndex = 0;

            do
            {
                // we use snapshots of backlog and completed to work without lock and
                // be able to modify the collections.
                backlogCopy.Clear();
                backlogCopy.AddRange(backlog);

                lock (_sync)
                {
                    if (completedIndex < _completed.Count)
                    {
                        for (var i = completedIndex; i < _completed.Count; i++)
                        {
                            completedCopy.Add(_completed[i]);
                        }

                        completedIndex = completedCopy.Count;
                    }
                }

                foreach (var executionNode in backlogCopy)
                {
                    if (DependenciesFulfilled(executionNode.DependsOn, completedCopy) &&
                        executionNode is RequestNode requestNode)
                    {
                        BeginExecuteNode(requestNode, state, semaphore, ct);
                        backlog.Remove(requestNode);
                    }
                }

                await semaphore.WaitAsync(ct);
            } while (backlog.Count > 0);
        }

        // wait for tasks to complete.
        while (_completedCount < plan.ExecutionNodes.Count)
        {
            await semaphore.WaitAsync(ct);
        }

        static bool DependenciesFulfilled(
            IReadOnlyList<ExecutionNode> dependsOn,
            HashSet<ExecutionNode> completed)
        {
            foreach (var dependency in dependsOn)
            {
                if (!completed.Contains(dependency))
                {
                    return false;
                }
            }

            return true;
        }
    }

#pragma warning disable CS4014
    private void BeginExecuteNode(
        RequestNode requestNode,
        IExecutionState state,
        SemaphoreSlim semaphore,
        CancellationToken ct)
        => ExecuteNode(requestNode, state, semaphore, ct);
#pragma warning restore CS4014

    private async Task ExecuteNode(
        RequestNode requestNode,
        IExecutionState state,
        SemaphoreSlim semaphore,
        CancellationToken ct)
    {
        try
        {
            var request = requestNode.Handler.CreateRequest(state);
            var executor = _executorFactory.Create(request.SchemaName);
            var response = await executor.ExecuteAsync(request, ct);

            BuildResult(response, null);

            lock (_sync)
            {
                _completed.Add(requestNode);
            }
        }
        finally
        {
            Interlocked.Increment(ref _completedCount);
            semaphore.Release();
        }
    }

    private void BuildResult(Response response, object responseNode)
    {

    }
}
