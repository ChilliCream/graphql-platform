using System.Buffers;
using System.Collections.Immutable;
using HotChocolate.Execution.DependencyInjection;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed class DeferTask : ExecutionTask
{
    private static readonly ArrayPool<IExecutionTask> s_pool = ArrayPool<IExecutionTask>.Shared;
    private OperationContext _parentContext = null!;
    private DeferExecutionCoordinator _coordinator = null!;
    private object? _parent;
    private IImmutableDictionary<string, object?> _scopedContext = null!;
    private SelectionSet _selectionSet = null!;
    private Path _selectionPath = null!;
    private int _executionBranchId;
    private DeferUsage _deferUsage = null!;

    // the defer tasks runs in the system branch as its just an orchestration task.
    public override int BranchId => BranchTracker.SystemBranchId;

    public override bool IsDeferred => true;

    protected override IExecutionTaskContext Context => _parentContext;

    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var contextFactory = _parentContext.Services.GetRequiredService<IFactory<OperationContextOwner>>();
        using var deferContextOwner = contextFactory.Create();
        var deferContext = deferContextOwner.OperationContext;

        // we first need to initialize the rented context for this defer operation.
        deferContext.InitializeDeferContext(
            _parentContext,
            _selectionSet,
            _selectionPath,
            _executionBranchId,
            _deferUsage);

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
            if (i > 1)
            {
                bufferedTasks.AsSpan(0, i).Clear();
            }

            s_pool.Return(bufferedTasks);
        }

        // ... and then wait for the scheduler to complete the deferred tasks.
        await deferContext.Scheduler.WaitForCompletionAsync(_executionBranchId);

        // once the execution branch has completed we enqueue the completed
        // result with the defer coordinator so it can be delivered.
        _coordinator.EnqueueResult(deferContext.BuildResult(), _executionBranchId);
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
        _parentContext = parentContext;
        _coordinator = parentContext.DeferExecutionCoordinator;
        _parent = parent;
        _scopedContext = scopedContext;
        _selectionSet = selectionSet;
        _executionBranchId = executionBranchId;
        _deferUsage = deferUsage;
        _selectionPath = selectionPath;
    }

    public new void Reset()
    {
        _parentContext = null!;
        _coordinator = null!;
        _parent = null!;
        _scopedContext = null!;
        _selectionSet = null!;
        _executionBranchId = 0;
        _deferUsage = null!;
        _selectionPath = null!;

        base.Reset();
    }
}
