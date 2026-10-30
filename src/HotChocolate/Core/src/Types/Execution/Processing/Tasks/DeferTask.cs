using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Execution.DependencyInjection;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed class DeferTask : ExecutionTask
{
    private static readonly ArrayPool<IExecutionTask> s_pool = ArrayPool<IExecutionTask>.Shared;
    private OperationContextOwner _deferContextOwner = null!;
    private object? _parent;
    private IImmutableDictionary<string, object?> _scopedContext = null!;
    private ExecutionGroupScope? _groupScope;
    private int _executionBranchId;
    private DeferUsage _deferUsage = null!;

    // the defer tasks runs in the system branch as it's just an orchestration task.
    public override int BranchId => BranchTracker.SystemBranchId;

    public override bool IsDeferred => true;

    protected override IExecutionTaskContext Context => _deferContextOwner.OperationContext;

    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var deferContext = _deferContextOwner.OperationContext;
        var data = deferContext.Result.Data.Data;
        var bufferedTasks = s_pool.Rent(data.GetPropertyCount());
        var i = 0;

        try
        {
            foreach (var field in data.EnumerateObject())
            {
                var selection = field.AssertSelection();
                var task =
                    deferContext.CreateResolverTask(
                        _parent,
                        selection,
                        field.Value,
                        _scopedContext,
                        _executionBranchId,
                        _deferUsage);

                task.JoinExecutionGroup(_groupScope);

                bufferedTasks[i++] = task;
            }

            // we register our deferred tasks for execution ...
            deferContext.Scheduler.Register(bufferedTasks.AsSpan(0, i));
        }
        finally
        {
            if (i > 0)
            {
                bufferedTasks.AsSpan(0, i).Clear();
            }

            s_pool.Return(bufferedTasks);
        }

        try
        {
            // ... and then wait for the scheduler to complete the deferred tasks.
            await deferContext.Scheduler.WaitForCompletionAsync(_executionBranchId);

            // once the execution branch has completed we enqueue the completed
            // result with the defer coordinator so it can be delivered.
            var result = deferContext.BuildResult();
            deferContext.DeferExecutionCoordinator.EnqueueResult(result, _executionBranchId);
        }
        finally
        {
            // the request cleanup registered by the deferred resolvers is handed to the collector of
            // the response stream, also when the branch faults or is cancelled, before the context is reset.
            var requestCleanup = deferContext.RequestCleanup!;

            foreach (var cleanupTask in deferContext.Result.TakeCleanupTasks())
            {
                requestCleanup.Register(cleanupTask);
            }
        }
    }

    protected override async ValueTask OnAfterCompletedAsync(CancellationToken cancellationToken)
    {
        // the branch can still be running when the request was aborted, so the defer context
        // is only given back once all of its tasks have been returned.
        await _deferContextOwner.OperationContext.WaitForOutstandingTasksAsync().ConfigureAwait(false);
        Reset();
    }

    public void Initialize(
        OperationContext parentContext,
        object? parent,
        IImmutableDictionary<string, object?> scopedContext,
        ExecutionGroupScope? groupScope,
        SelectionSet selectionSet,
        Path selectionPath,
        int executionBranchId,
        DeferUsage deferUsage)
    {
        var contextFactory = parentContext.Services.GetRequiredService<IFactory<OperationContextOwner>>();
        _deferContextOwner = contextFactory.Create();

        // we first need to initialize the rented context for this defer operation.
        _deferContextOwner.OperationContext.InitializeDeferContext(
            parentContext,
            selectionSet,
            selectionPath,
            executionBranchId,
            deferUsage);

        _parent = parent;
        _scopedContext = scopedContext;
        _groupScope = groupScope;
        _executionBranchId = executionBranchId;
        _deferUsage = deferUsage;
    }

    public new void Reset()
    {
        _deferContextOwner.Dispose();
        _deferContextOwner = null!;
        _parent = null!;
        _scopedContext = null!;
        _groupScope = null;
        _executionBranchId = 0;
        _deferUsage = null!;

        base.Reset();
    }
}
