using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Completes an execution node whose execution finishes asynchronously.
/// An instance serves one execution at a time and is reusable once the node completed.
/// </summary>
internal sealed class ExecutionNodeContinuation
{
    private static readonly ContextCallback s_complete =
        static state => Unsafe.As<ExecutionNodeContinuation>(state)!.Complete();

    private readonly Action _onCompleted;
    private ConfiguredValueTaskAwaitable<ExecutionStatus>.ConfiguredValueTaskAwaiter _awaiter;
    private ExecutionContext? _executionContext;
    private OperationPlanContext? _context;
    private ExecutionNode? _node;
    private IDisposable? _scope;
    private Activity? _activity;
    private long _start;

    public ExecutionNodeContinuation()
    {
        _onCompleted = OnCompleted;
    }

    public void Await(
        OperationPlanContext context,
        ExecutionNode node,
        IDisposable? scope,
        long start,
        Activity? activity,
        ValueTask<ExecutionStatus> execution)
    {
        _context = context;
        _node = node;
        _scope = scope;
        _start = start;
        _activity = activity;
        _executionContext = ExecutionContext.Capture();
        _awaiter = execution.ConfigureAwait(false).GetAwaiter();
        _awaiter.UnsafeOnCompleted(_onCompleted);
    }

    private void OnCompleted()
    {
        var executionContext = _executionContext;

        if (executionContext is null)
        {
            Complete();
        }
        else
        {
            ExecutionContext.Run(executionContext, s_complete, this);
        }
    }

    private void Complete()
    {
        var context = _context!;
        var node = _node!;
        var scope = _scope;
        var start = _start;
        var activity = _activity;
        ExecutionStatus status;
        Exception? error = null;

        try
        {
            status = _awaiter.GetResult();
        }
        catch (Exception ex)
        {
            status = ExecutionStatus.Failed;
            error = ex;
        }

        // The node completion can start the next execution that uses this instance,
        // so the instance is released before the node completes.
        _awaiter = default;
        _executionContext = null;
        _context = null;
        _node = null;
        _scope = null;
        _activity = null;
        _start = 0;

        node.EndExecute(context, scope, start, activity, status, error);
    }
}
