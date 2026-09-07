using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
/// Delivers the items of a streamed list field that are not part of the initial slice.
/// The task owns the live enumerator and the cleanup tasks of the resolver that produced it.
/// </summary>
internal sealed class StreamTask : ExecutionTask
{
    private readonly MiddlewareContext _itemContext = new();
    private readonly List<IExecutionTask> _taskBuffer = [];
    private OperationContextOwner _streamContextOwner = null!;
    private IAsyncEnumerator<object?> _enumerator = null!;
    private Selection _selection = null!;
    private IType _elementType = null!;
    private Path _path = null!;
    private IImmutableDictionary<string, object?> _scopedContext = null!;
    private ImmutableArray<Func<ValueTask>> _cleanupTasks = [];
    private DeferUsage? _deferUsage;
    private object? _parent;
    private object? _lookAheadItem;
    private int _executionBranchId;
    private int _nextIndex;

    // the stream task runs in the system branch as it's just an orchestration task.
    public override int BranchId => BranchTracker.SystemBranchId;

    public override bool IsDeferred => true;

    protected override IExecutionTaskContext Context => _streamContextOwner.OperationContext;

    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var streamContext = _streamContextOwner.OperationContext;
        var coordinator = streamContext.DeferExecutionCoordinator;
        var scheduler = streamContext.Scheduler;
        var item = _lookAheadItem;
        var index = _nextIndex;
        var hasItem = true;

        // the look-ahead item was pulled by the resolver task and is not needed after this point.
        _lookAheadItem = null;

        try
        {
            // Items are processed strictly sequentially: the next item is only pulled once the
            // subtree of the current item has completed and its chunk was enqueued.
            while (hasItem && !cancellationToken.IsCancellationRequested)
            {
                streamContext.InitializeStreamItem(_selection, _path.Append(index));

                CompleteItem(streamContext, item);

                await scheduler.WaitForCompletionAsync(_executionBranchId).ConfigureAwait(false);
                await coordinator
                    .EnqueueStreamItem(streamContext.BuildStreamItemResult(), _executionBranchId)
                    .ConfigureAwait(false);

                index++;
                hasItem = await _enumerator.MoveNextAsync().ConfigureAwait(false);
                item = hasItem ? _enumerator.Current : null;
            }
        }
        finally
        {
            // when the request was aborted nothing is emitted anymore as there is no consumer left.
            if (!cancellationToken.IsCancellationRequested)
            {
                coordinator.CompleteStream(_executionBranchId);
            }

            await _enumerator.DisposeAsync().ConfigureAwait(false);
            await ExecuteCleanupTasksAsync().ConfigureAwait(false);
        }
    }

    private void CompleteItem(OperationContext streamContext, object? item)
    {
        var itemValue = streamContext.Result.Data.Data;

        // the item context is reused for every item, so we reset it before we rebind it
        // to the result slot of the current item.
        _itemContext.Clean();
        _itemContext.Initialize(_parent, _selection, itemValue, streamContext, _deferUsage, _scopedContext);
        _itemContext.BranchId = _executionBranchId;

        try
        {
            var completionContext =
                new ValueCompletionContext(streamContext, _itemContext, _taskBuffer, _executionBranchId);
            ValueCompletion.Complete(completionContext, _selection, _elementType, itemValue, item);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            streamContext.ReportError(ex, _itemContext, _selection, itemValue.Path);
            itemValue.SetNullValue();
        }

        switch (_taskBuffer.Count)
        {
            case 0:
                break;

            case 1:
                streamContext.Scheduler.Register(_taskBuffer[0]);
                break;

            default:
                streamContext.Scheduler.Register(CollectionsMarshal.AsSpan(_taskBuffer));
                break;
        }

        _taskBuffer.Clear();
    }

    private async ValueTask ExecuteCleanupTasksAsync()
    {
        if (_cleanupTasks.IsEmpty)
        {
            return;
        }

        foreach (var cleanupTask in _cleanupTasks)
        {
            await cleanupTask().ConfigureAwait(false);
        }
    }

    protected override ValueTask OnAfterCompletedAsync(CancellationToken cancellationToken)
    {
        Reset();
        return ValueTask.CompletedTask;
    }

    public void Initialize(
        OperationContext parentContext,
        object? parent,
        Selection selection,
        Path path,
        IImmutableDictionary<string, object?> scopedContext,
        DeferUsage? deferUsage,
        int executionBranchId,
        IAsyncEnumerator<object?> enumerator,
        object? lookAheadItem,
        int nextIndex,
        ImmutableArray<Func<ValueTask>> cleanupTasks)
    {
        var contextFactory = parentContext.Services.GetRequiredService<IFactory<OperationContextOwner>>();
        _streamContextOwner = contextFactory.Create();

        // we first need to initialize the rented context for this stream.
        _streamContextOwner.OperationContext.InitializeStreamContext(parentContext, executionBranchId);

        _parent = parent;
        _selection = selection;
        _elementType = selection.Type.ElementType();
        _path = path;
        _scopedContext = scopedContext;
        _deferUsage = deferUsage;
        _executionBranchId = executionBranchId;
        _enumerator = enumerator;
        _lookAheadItem = lookAheadItem;
        _nextIndex = nextIndex;
        _cleanupTasks = cleanupTasks;
    }

    public new void Reset()
    {
        _itemContext.Clean();
        _taskBuffer.Clear();
        _streamContextOwner.Dispose();
        _streamContextOwner = null!;
        _enumerator = null!;
        _selection = null!;
        _elementType = null!;
        _path = null!;
        _scopedContext = null!;
        _cleanupTasks = [];
        _deferUsage = null;
        _parent = null;
        _lookAheadItem = null;
        _executionBranchId = 0;
        _nextIndex = 0;

        base.Reset();
    }
}
