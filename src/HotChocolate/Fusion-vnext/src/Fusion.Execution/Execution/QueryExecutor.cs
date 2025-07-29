using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class QueryExecutor
{
    public async ValueTask<IExecutionResult> QueryAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        context.Begin();
        await ExecuteAsync(new QueryContext(context, cancellationToken));
        return context.Complete();
    }

    private static async ValueTask ExecuteAsync(QueryContext context)
    {
        context.Start();

        while (context.IsProcessing)
        {
            await context.WaitForNextCompletionAsync();
            EnqueueNextNodes(context);
        }
    }

    private static void EnqueueNextNodes(QueryContext context)
    {
        foreach (var node in context.WaitingToRun)
        {
            var dependenciesFulfilled = true;

            foreach (var dependency in node.Dependencies)
            {
                if (context.IsCompleted(dependency))
                {
                    continue;
                }

                dependenciesFulfilled = false;
            }

            if (dependenciesFulfilled)
            {
                context.StartNode(node);
            }
        }
    }

    private sealed class QueryContext
    {
        private readonly List<ExecutionNode> _stack = [];
        private readonly HashSet<ExecutionNode> _completed = [];
        private readonly HashSet<Task<ExecutionNodeResult>> _activeTasks = [];
        private readonly List<Task<ExecutionNodeResult>> _completedTasks = [];
        private readonly List<ExecutionNode> _backlog;
        private readonly OperationPlanContext  _context;
        private readonly OperationPlan _plan;
        private readonly List<ExecutionNodeTrace> _traces = [];
        private readonly CancellationToken _cancellationToken;

        public QueryContext(OperationPlanContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
            _plan = context.OperationPlan;
            _backlog = [.. context.OperationPlan.AllNodes];
        }

        public ReadOnlySpan<ExecutionNode> WaitingToRun
            => CollectionsMarshal.AsSpan(_backlog);

        public bool IsProcessing => _backlog.Count > 0 || _activeTasks.Count > 0;

        public bool IsCompleted(ExecutionNode node)
            => _completed.Contains(node);

        private void SkipNode(ExecutionNode node)
        {
            _stack.Clear();
            _stack.Push(node);

            while (_stack.TryPop(out var current))
            {
                _backlog.Remove(current);

                foreach (var enqueuedNode in WaitingToRun)
                {
                    if (enqueuedNode.Dependencies.Contains(current))
                    {
                        _stack.Push(enqueuedNode);
                    }
                }
            }
        }

        public void Start()
        {
            foreach (var node in _context.OperationPlan.RootNodes)
            {
                StartNode(node);
            }
        }

        public void StartNode(ExecutionNode node)
        {
            _backlog.Remove(node);
            _activeTasks.Add(node.ExecuteAsync(_context, _cancellationToken));
        }

        public async Task WaitForNextCompletionAsync()
        {
            await Task.WhenAny(_activeTasks);

            foreach (var task in _activeTasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    var node = _plan.GetNodeById(task.Result.Id);

                    _completedTasks.Add(task);
                    _completed.Add(node);
                    _traces.Add(new ExecutionNodeTrace
                    {
                        Id = task.Result.Id,
                        SpanId = task.Result.Activity?.Id,
                        Status = task.Result.Status,
                        Duration = task.Result.Duration
                    });

                    if (task.Result.Status is ExecutionStatus.Skipped or ExecutionStatus.Failed)
                    {
                        SkipNode(node);
                    }
                }
            }

            foreach (var task in _completedTasks)
            {
                _activeTasks.Remove(task);
            }

            _completedTasks.Clear();

            if (_backlog.Count == 0)
            {
                _context.Traces = [.._traces];
            }
        }
    }
}
