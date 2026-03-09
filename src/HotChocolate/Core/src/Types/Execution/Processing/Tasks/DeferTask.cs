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
                bufferedTasks[i++] =
                    deferContext.CreateResolverTask(
                        _parent,
                        field.AssertSelection(),
                        field.Value,
                        _scopedContext,
                        _executionBranchId,
                        _deferUsage);
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

        // ... and then wait for the scheduler to complete the deferred tasks.
        await deferContext.Scheduler.WaitForCompletionAsync(_executionBranchId);

        // once the execution branch has completed we enqueue the completed
        // result with the defer coordinator so it can be delivered.
        deferContext.DeferExecutionCoordinator.EnqueueResult(deferContext.BuildResult(), _executionBranchId);
    }

    protected override ValueTask OnAfterCompletedAsync(CancellationToken cancellationToken)
    {
        // TODO : we need to give the context back here and not rest once we have a pool.
        Reset();
        return ValueTask.CompletedTask;
    }

    public void Initialize(
        OperationContext parentContext,
        object? parent,
        IImmutableDictionary<string, object?> scopedContext,
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
        _executionBranchId = executionBranchId;
        _deferUsage = deferUsage;
    }

    public new void Reset()
    {
        _deferContextOwner.Dispose();
        _deferContextOwner = null!;
        _parent = null!;
        _scopedContext = null!;
        _executionBranchId = 0;
        _deferUsage = null!;

        base.Reset();
    }
}
