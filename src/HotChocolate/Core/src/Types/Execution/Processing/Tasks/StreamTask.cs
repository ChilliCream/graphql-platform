using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Text.Json;
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
        IReadOnlyList<IError>? completionErrors = null;

        // the look-ahead item was pulled by the resolver task and is not needed after this point.
        _lookAheadItem = null;

        // the stream ends as soon as the request is aborted or the branch this task delivers to
        // was aborted because the data it was rooted in was removed from the response.
        using var streamAborted = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            coordinator.GetStreamAbortToken(_executionBranchId));
        var streamToken = streamAborted.Token;

        try
        {
            // Items are processed strictly sequentially: the next item is only pulled once the
            // subtree of the current item has completed and its chunk was enqueued.
            while (hasItem && !streamToken.IsCancellationRequested)
            {
                streamContext.InitializeStreamItem(_selection, _path.Append(index));

                var itemValue = streamContext.Result.Data.Data;

                CompleteItem(streamContext, itemValue, item);

                await scheduler.WaitForCompletionAsync(_executionBranchId).ConfigureAwait(false);

                if (streamToken.IsCancellationRequested)
                {
                    // there is no consumer left for this chunk, so it is dropped.
                    streamContext.Result.Data.Dispose();
                    break;
                }

                if (HasNullBubbledOutOfItem(streamContext, itemValue))
                {
                    // the null bubbled out of the item, so the chunk is never delivered and its
                    // errors move to the completed entry that ends the stream.
                    completionErrors = streamContext.BuildStreamItemErrors();
                    streamContext.Result.Data.Dispose();
                    break;
                }

                await coordinator
                    .EnqueueStreamItem(streamContext.BuildStreamItemResult(), _executionBranchId)
                    .ConfigureAwait(false);

                index++;

                try
                {
                    hasItem = await _enumerator.MoveNextAsync().ConfigureAwait(false);
                    item = hasItem ? _enumerator.Current : null;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // a failing source ends the stream in any error handling mode.
                    completionErrors = CreateSourceErrors(streamContext, ex);
                    hasItem = false;
                }
            }
        }
        finally
        {
            // when the request or the branch was aborted nothing is emitted anymore
            // as there is no consumer left.
            if (!streamToken.IsCancellationRequested)
            {
                coordinator.CompleteStream(_executionBranchId, completionErrors);
            }

            await _enumerator.DisposeAsync().ConfigureAwait(false);
            await ExecuteCleanupTasksAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Determines whether the null of the completed item bubbled out of the item and therefore
    /// removed the item from the streamed list.
    /// </summary>
    private static bool HasNullBubbledOutOfItem(OperationContext streamContext, ResultElement itemValue)
    {
        // a null that was propagated into the item leaves the item value itself null, while a null
        // that was propagated out of a completed item object invalidates that object.
        return streamContext.PropagateNullValues
            && itemValue is { IsNullable: false }
            && (itemValue.IsNullOrInvalidated || itemValue.IsInvalidated);
    }

    /// <summary>
    /// Reports the source failure on the list path through the standard error pipeline and
    /// returns the errors that end the stream.
    /// </summary>
    private IReadOnlyList<IError> CreateSourceErrors(OperationContext streamContext, Exception exception)
    {
        // the errors of the last item were delivered with its chunk, so the result builder is
        // reset to collect the source errors on their own.
        streamContext.Result.Reset();
        streamContext.ReportError(exception, _itemContext, _selection, _path);
        return streamContext.Result.Errors;
    }

    private void CompleteItem(OperationContext streamContext, ResultElement itemValue, object? item)
    {
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

        if (itemValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            var itemPath = itemValue.Path;

            if (streamContext.PropagateNullValues)
            {
                ValueCompletion.PropagateNullValues(itemValue);
            }
            else
            {
                itemValue.SetNullValue();
            }

            streamContext.Result.AddNonNullViolation(itemPath);
            _taskBuffer.Clear();
            return;
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
