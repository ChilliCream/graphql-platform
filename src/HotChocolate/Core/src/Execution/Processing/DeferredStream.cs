using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents the work to executed the deferred elements of a stream.
/// </summary>
internal sealed class DeferredStream : IDeferredExecutionTask
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
    {
        Selection = selection;
        Label = label;
        Path = path;
        Parent = parent;
        Index = index;
        Enumerator = enumerator;
        ScopedContextData = scopedContextData;
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

    /// <summary>
    /// Gets the preserved scoped context from the parent resolver.
    /// </summary>
    public IImmutableDictionary<string, object?> ScopedContextData { get; }

    /// <inheritdoc/>
    public IDeferredExecutionTask? Next { get; set; }

    /// <inheritdoc/>
    public IDeferredExecutionTask? Previous { get; set; }

    /// <inheritdoc/>
    public async Task<IQueryResult?> ExecuteAsync(IOperationContext operationContext)
    {
        _task ??= new StreamExecutionTask(operationContext, this);
        _task.Reset();

        operationContext.QueryPlan = operationContext.QueryPlan.GetStreamPlan(Selection.Id);
        operationContext.Scheduler.Register(_task);
        await operationContext.Scheduler.ExecuteAsync().ConfigureAwait(false);

        if (_task.ChildTask is null)
        {
            return null;
        }

        operationContext.Scheduler.DeferredWork.Register(this);

        IQueryResult result = operationContext
            .TrySetNext(true)
            .SetLabel(Label)
            .SetPath(operationContext.PathFactory.Append(Path, Index))
            .SetData((ResultMap)_task.ChildTask.ResultMap[0].Value!)
            .BuildResult();

        _task.ChildTask.CompleteUnsafe();

        return result;
    }


    private sealed class StreamExecutionTask : ExecutionTask
    {
        private readonly IOperationContext _operationContext;
        private readonly DeferredStream _deferredStream;

        public StreamExecutionTask(IOperationContext operationContext, DeferredStream deferredStream)
        {
            _operationContext = operationContext;
            _deferredStream = deferredStream;
            Context = (IExecutionTaskContext)operationContext;
        }

        protected override IExecutionTaskContext Context { get; }

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
                    _deferredStream.ScopedContextData);
            }
        }

        public new void Reset() => base.Reset();
    }
}
