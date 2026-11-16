using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Text.Json;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
/// Streams the remaining items of a list field after its initial items were completed
/// and delivers them in batches through the defer coordinator.
/// </summary>
internal sealed class StreamTask : ExecutionTask, IAsyncDisposable
{
    private const int BufferedBatches = 4;

    private readonly OperationContext _parentContext;
    private readonly Selection _selection;
    private readonly object? _parent;
    private readonly IImmutableDictionary<string, object?> _scopedContext;
    private readonly DeferUsage? _deferUsage;
    private readonly Path _path;
    private readonly string? _label;
    private readonly IAsyncEnumerator<object?> _enumerator;
    private readonly CancellationTokenSource _cancellation;
    private readonly CancellationToken _cancellationToken;
    private readonly int _maxItemsPerPayload;
    private readonly Channel<object?> _buffer;
    private readonly MiddlewareContext _resolverContext = new();
    private int _nextItemIndex;
    private int _streamBranchId;

    public StreamTask(
        OperationContext parentContext,
        Selection selection,
        object? parent,
        IImmutableDictionary<string, object?> scopedContext,
        DeferUsage? deferUsage,
        Path path,
        string? label,
        int firstStreamedIndex,
        int maxItemsPerPayload,
        IAsyncEnumerator<object?> enumerator,
        CancellationTokenSource cancellation)
    {
        _parentContext = parentContext;
        _selection = selection;
        _parent = parent;
        _scopedContext = scopedContext;
        _deferUsage = deferUsage;
        _path = path;
        _label = label;
        _nextItemIndex = firstStreamedIndex;
        _maxItemsPerPayload = maxItemsPerPayload;
        _buffer = Channel.CreateBounded<object?>(
            new BoundedChannelOptions((int)Math.Min((long)maxItemsPerPayload * BufferedBatches, int.MaxValue))
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });
        _enumerator = enumerator;
        _cancellation = cancellation;
        _cancellationToken = cancellation.Token;
    }

    // the stream task runs in the system branch as it's just an orchestration task.
    public override int BranchId => BranchTracker.SystemBranchId;

    public override bool IsDeferred => true;

    protected override IExecutionTaskContext Context => _parentContext;

    /// <summary>
    /// Announces the stream with the payload of the specified branch.
    /// </summary>
    public void Announce(int parentBranchId, ResultElement list)
        => _streamBranchId = _parentContext.DeferExecutionCoordinator.BranchStream(
            parentBranchId,
            _path,
            _label,
            list,
            this);

    /// <summary>
    /// Starts reading the items of the source into the buffer of the stream.
    /// </summary>
    public void StartProducer()
        => _ = ProduceAsync();

    /// <summary>
    /// Stops the stream. The enumerator is disposed by the producer once the source returns control.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Cancel();
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await StreamAsync().ConfigureAwait(false);
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task ProduceAsync()
    {
        var writer = _buffer.Writer;

        try
        {
            while (await _enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                await writer.WriteAsync(_enumerator.Current, _cancellationToken).ConfigureAwait(false);
            }

            writer.Complete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
        finally
        {
            try
            {
                await _enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // the stream already ended or was discarded, so a failing disposal has nobody to report to.
            }
            finally
            {
                _cancellation.Dispose();
            }
        }
    }

    private async ValueTask StreamAsync()
    {
        var coordinator = _parentContext.DeferExecutionCoordinator;
        var token = _cancellationToken;
        var reader = _buffer.Reader;
        var items = new List<object?>();

        try
        {
            while (true)
            {
                if (!reader.TryRead(out var item))
                {
                    if (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                    {
                        continue;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        coordinator.CompleteStream(_streamBranchId, errors: null);
                    }

                    return;
                }

                items.Clear();
                items.Add(item);

                while (items.Count < _maxItemsPerPayload && reader.TryRead(out item))
                {
                    items.Add(item);
                }

                var completesStream = reader.Completion.IsCompletedSuccessfully;

                if (token.IsCancellationRequested
                    || !await DeliverAsync(items, completesStream).ConfigureAwait(false)
                    || completesStream)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // the stream was stopped.
        }
        catch (Exception ex)
        {
            coordinator.CompleteStream(_streamBranchId, CreateErrors(ex));
        }
    }

    private async ValueTask<bool> DeliverAsync(List<object?> items, bool completesStream)
    {
        var coordinator = _parentContext.DeferExecutionCoordinator;
        var itemsBranchId = BranchTracker.SystemBranchId;
        OperationContextOwner? contextOwner = null;
        var tasks = new List<IExecutionTask>();
        var tasksRegistered = false;

        try
        {
            itemsBranchId = coordinator.BranchStreamItems(_streamBranchId);

            if (itemsBranchId == BranchTracker.SystemBranchId)
            {
                return false;
            }

            var contextFactory = _parentContext.Services.GetRequiredService<IFactory<OperationContextOwner>>();
            contextOwner = contextFactory.Create();
            var context = contextOwner.OperationContext;

            context.InitializeStreamContext(_parentContext, _selection, _path.Parent, _nextItemIndex, itemsBranchId);
            _nextItemIndex += items.Count;

            try
            {
                var listResult = await CompleteItemsAsync(context, items, tasks, itemsBranchId)
                    .ConfigureAwait(false);

                if (tasks.Count > 0)
                {
                    context.Scheduler.Register(CollectionsMarshal.AsSpan(tasks));
                    tasksRegistered = true;
                }

                await context.Scheduler.WaitForCompletionAsync(itemsBranchId).ConfigureAwait(false);

                var result = context.BuildStreamItemsResult();

                if (listResult.IsNullOrInvalidated || result.Data is { IsValueNull: true })
                {
                    await coordinator.ReleaseChildBranches(itemsBranchId).ConfigureAwait(false);
                    await coordinator.EnqueueStreamItems(
                            result,
                            itemsBranchId,
                            hasItems: false,
                            completesStream: completesStream)
                        .ConfigureAwait(false);
                    return false;
                }

                await coordinator.EnqueueStreamItems(
                        result,
                        itemsBranchId,
                        hasItems: true,
                        completesStream: completesStream)
                    .ConfigureAwait(false);
                return true;
            }
            finally
            {
                // the cleanup registered by the item resolvers is handed to the response stream, also when
                // the batch faults or is cancelled, before the context is returned.
                foreach (var cleanupTask in context.Result.TakeCleanupTasks())
                {
                    _parentContext.RegisterRequestCleanup(cleanupTask);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException
            || !_cancellationToken.IsCancellationRequested)
        {
            if (itemsBranchId == BranchTracker.SystemBranchId)
            {
                coordinator.CompleteStream(_streamBranchId, CreateErrors(ex));
                return false;
            }

            if (tasks.Count > 0 && !tasksRegistered)
            {
                await coordinator.ReleaseChildBranches(itemsBranchId).ConfigureAwait(false);

                foreach (var task in tasks)
                {
                    if (task is DeferTask deferTask)
                    {
                        deferTask.Reset();
                    }
                }
            }

            var result = new OperationResult(CreateErrors(ex));
            await coordinator.EnqueueStreamItems(result, itemsBranchId, hasItems: false, completesStream: true)
                .ConfigureAwait(false);
            return false;
        }
        finally
        {
            contextOwner?.Dispose();
        }
    }

    private async ValueTask<ResultElement> CompleteItemsAsync(
        OperationContext context,
        List<object?> items,
        List<IExecutionTask> tasks,
        int itemsBranchId)
    {
        var listResult = default(ResultElement);

        foreach (var property in context.Result.Data.Data.EnumerateObject())
        {
            listResult = property.Value;
        }

        _resolverContext.Initialize(_parent, _selection, listResult, context, _deferUsage, _scopedContext);
        _resolverContext.BranchId = itemsBranchId;

        try
        {
            var completionContext = new ValueCompletionContext(context, _resolverContext, tasks, groupScope: null);
            Complete(completionContext, _selection, listResult, items);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.ReportError(ex, _resolverContext, listResult.Path);
            listResult.SetNullValue();
        }
        finally
        {
            _resolverContext.Clean();
        }

        if (listResult is { IsNullable: false, IsNullOrInvalidated: true })
        {
            if (context.PropagateNullValues)
            {
                PropagateNullValues(listResult);
            }
            else
            {
                listResult.SetNullValue();
            }

            context.Result.AddNonNullViolation(listResult.Path);

            await _parentContext.DeferExecutionCoordinator.ReleaseChildBranches(itemsBranchId)
                .ConfigureAwait(false);

            foreach (var task in tasks)
            {
                if (task is DeferTask deferTask)
                {
                    deferTask.Reset();
                }
            }

            tasks.Clear();
        }

        return listResult;
    }

    private ImmutableList<IError> CreateErrors(Exception exception)
    {
        var errors = ImmutableList.CreateBuilder<IError>();

        if (exception is GraphQLException graphQLException)
        {
            foreach (var error in graphQLException.Errors)
            {
                Add(error.Path is null ? error.WithPath(_path) : error);
            }
        }
        else
        {
            Add(ErrorHelper.StreamEnumerationFailed(exception, _path));
        }

        return errors.ToImmutable();

        void Add(IError error)
        {
            var handled = _parentContext.ErrorHandler.Handle(error);

            if (handled is AggregateError aggregateError)
            {
                errors.AddRange(aggregateError.Errors);
            }
            else
            {
                errors.Add(handled);
            }
        }
    }

    private void Cancel()
    {
        try
        {
            _cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // the stream already ended and released its resources.
        }
    }
}
