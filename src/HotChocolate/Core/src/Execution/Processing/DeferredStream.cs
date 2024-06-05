using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Tasks;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents the work to executed the deferred elements of a stream.
/// </summary>
internal sealed class DeferredStream : DeferredExecutionTask
{
    private StreamExecutionTask? _task;

    /// <summary>
    /// Initializes a new instance of <see cref="DeferredFragment"/>.
    /// </summary>
    public DeferredStream(
        ISelection selection,
        string? label,
        Path path,
        object? parent,
        int index,
        IAsyncEnumerator<object?> enumerator,
        IImmutableDictionary<string, object?> scopedContextData)
        : base(scopedContextData)
    {
        Selection = selection;
        Label = label;
        Path = path;
        Parent = parent;
        Index = index;
        Enumerator = enumerator;
    }

    /// <summary>
    /// Gets the selection of the streamed field.
    /// </summary>
    public ISelection Selection { get; }

    /// <summary>
    /// If this argument label has a value other than null, it will be passed
    /// on to the result of this defer directive. This label is intended to
    /// give client applications a way to identify to which fragment a deferred
    /// result belongs to.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets the result path into which this deferred fragment shall be patched.
    /// </summary>
    public Path Path { get; }

    /// <summary>
    /// Gets the index of the last element.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Gets the parent / source value.
    /// </summary>
    public object? Parent { get; }

    /// <summary>
    /// Gets the enumerator to retrieve the payloads of the stream.
    /// </summary>
    public IAsyncEnumerator<object?> Enumerator { get; }

    protected override async Task ExecuteAsync(
        OperationContextOwner operationContextOwner,
        uint resultId,
        uint parentResultId,
        uint patchId)
    {
        var operationContext = operationContextOwner.OperationContext;

        try
        {
            _task ??= new StreamExecutionTask(this);
            _task.Reset(operationContext, resultId);
            operationContext.Scheduler.Register(_task);
            await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);

            // if there is no child task, then there is no more data, so we can complete.
            if (_task.ChildTask is null)
            {
                operationContext.DeferredScheduler.Complete(new(resultId, parentResultId));
                return;
            }

            var item = _task.ChildTask.ParentResult[0].Value!;

            var result = operationContext
                .SetLabel(Label)
                .SetPath(Path.Append(Index))
                .SetItems(new[] { item, })
                .SetPatchId(patchId)
                .BuildResult();

            await _task.ChildTask.CompleteUnsafeAsync().ConfigureAwait(false);

            // we will register this same task again to get the next item.
            operationContext.DeferredScheduler.Register(this, patchId);
            operationContext.DeferredScheduler.Complete(new(resultId, parentResultId, result));
        }
        catch (Exception ex)
        {
            var builder = operationContext.ErrorHandler.CreateUnexpectedError(ex);
            var result = OperationResultBuilder.CreateError(builder.Build());
            operationContext.DeferredScheduler.Complete(new(resultId, parentResultId, result));
        }
        finally
        {
            operationContextOwner.Dispose();
        }
    }

    private sealed class StreamExecutionTask : ExecutionTask
    {
        private readonly DeferredStream _deferredStream;
        private OperationContext _operationContext = default!;
        private IImmutableDictionary<string, object?> _scopedContextData;

        public StreamExecutionTask(DeferredStream deferredStream)
        {
            _deferredStream = deferredStream;
            _scopedContextData = _deferredStream.ScopedContextData;
        }

        protected override IExecutionTaskContext Context => _operationContext;

        public ResolverTask? ChildTask { get; private set; }

        protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
        {
            ChildTask = null;
            _deferredStream.Index++;
            var hasNext = await _deferredStream.Enumerator.MoveNextAsync();

            if (hasNext)
            {
                ChildTask = ResolverTaskFactory.EnqueueElementTasks(
                    _operationContext,
                    _deferredStream.Selection,
                    _deferredStream.Parent,
                    _deferredStream.Path,
                    _deferredStream.Index,
                    _deferredStream.Enumerator,
                    _scopedContextData);
            }
        }

        public void Reset(OperationContext operationContext, uint taskId)
        {
            _operationContext = operationContext;
            _scopedContextData = _scopedContextData.SetItem(DeferredResultId, taskId);
            base.Reset();
        }
    }
}
