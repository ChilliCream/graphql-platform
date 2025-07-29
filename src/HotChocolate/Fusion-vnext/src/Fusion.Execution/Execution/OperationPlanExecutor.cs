using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanExecutor
{
    public async Task<IExecutionResult> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        context.Begin();
        await ExecutorSession.ExecuteAsync(context, cancellationToken);
        return context.Complete();
    }

    private sealed class ExecutorSession
    {
        private readonly List<ExecutionNode> _stack = [];
        private readonly HashSet<ExecutionNode> _completed = [];
        private readonly HashSet<Task<ExecutionNodeResult>> _activeTasks = [];
        private readonly List<Task<ExecutionNodeResult>> _completedTasks = [];
        private readonly List<ExecutionNode> _backlog;
        private readonly OperationPlanContext _context;
        private readonly OperationPlan _plan;
        private readonly ImmutableArray<ExecutionNodeTrace>.Builder? _traces;
        private readonly CancellationToken _cancellationToken;

        private ExecutorSession(OperationPlanContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _cancellationToken = cancellationToken;
            _plan = context.OperationPlan;
            _backlog = [.. context.OperationPlan.AllNodes];
            var collectTracing = context.Schema.GetRequestOptions().CollectOperationPlanTelemetry;
            _traces = collectTracing ? ImmutableArray.CreateBuilder<ExecutionNodeTrace>() : null;
        }

        public static Task ExecuteAsync(OperationPlanContext context, CancellationToken cancellationToken)
            => new ExecutorSession(context, cancellationToken).ExecuteInternalAsync();

        private async Task ExecuteInternalAsync()
        {
            Start();

            while (IsProcessing())
            {
                await WaitForNextCompletionAsync();
                EnqueueNextNodes();
            }
        }

        private ReadOnlySpan<ExecutionNode> WaitingToRun
            => CollectionsMarshal.AsSpan(_backlog);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsProcessing() => _backlog.Count > 0 || _activeTasks.Count > 0;

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

        private void Start()
        {
            foreach (var node in _context.OperationPlan.RootNodes)
            {
                StartNode(node);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartNode(ExecutionNode node)
        {
            _backlog.Remove(node);
            _activeTasks.Add(node.ExecuteAsync(_context, _cancellationToken));
        }

        private async Task WaitForNextCompletionAsync()
        {
            await Task.WhenAny(_activeTasks);

            foreach (var task in _activeTasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    var node = _plan.GetNodeById(task.Result.Id);

                    _completedTasks.Add(task);
                    _completed.Add(node);
                    _traces?.Add(new ExecutionNodeTrace
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
                else if (task.IsFaulted || task.IsCanceled)
                {
                    // execution nodes are not expected to throw as exception should be handled within.
                    // if they do its a fatal error for the execution, so we await failed task here
                    // so that they can throw and terminate the execution.
                    await task;
                }
            }

            foreach (var task in _completedTasks)
            {
                _activeTasks.Remove(task);
            }

            _completedTasks.Clear();

            if (_backlog.Count == 0 && _traces is { Count: > 0 })
            {
                _context.Traces = [.. _traces];
            }
        }

        private void EnqueueNextNodes()
        {
            foreach (var node in WaitingToRun)
            {
                var dependenciesFulfilled = true;

                foreach (var dependency in node.Dependencies)
                {
                    if (_completed.Contains(dependency))
                    {
                        continue;
                    }

                    dependenciesFulfilled = false;
                }

                if (dependenciesFulfilled)
                {
                    StartNode(node);
                }
            }
        }
    }
}
