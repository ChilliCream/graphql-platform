using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
/// An execution task that collects multiple parent contexts for a batch resolver
/// and executes them in a single invocation.
/// </summary>
internal sealed class BatchResolverTask : ExecutionTask
{
    private readonly List<BatchEntry> _entries = [];
    private readonly List<IExecutionTask> _taskBuffer = [];
    private OperationContext _operationContext = null!;
    private BatchFieldDelegate _pipeline = null!;
    private Selection _selection = null!;
    private int _branchId;

    /// <inheritdoc />
    public override int BranchId => _branchId;

    /// <inheritdoc />
    public override bool IsDeferred => false;

    /// <inheritdoc />
    protected override IExecutionTaskContext Context => _operationContext;

    /// <summary>
    /// Gets the batch selection path that identifies this batch in the scheduler.
    /// </summary>
    internal BatchSelectionPath SelectionPath { get; private set; } = null!;

    /// <summary>
    /// Gets the number of entries currently collected in this batch.
    /// </summary>
    internal int EntryCount => _entries.Count;

    /// <summary>
    /// Initializes this batch task.
    /// </summary>
    public void Initialize(
        OperationContext operationContext,
        Selection selection,
        BatchFieldDelegate pipeline,
        BatchSelectionPath selectionPath,
        int branchId)
    {
        _operationContext = operationContext;
        _selection = selection;
        _pipeline = pipeline;
        _branchId = branchId;
        SelectionPath = selectionPath;
    }

    /// <summary>
    /// Adds a parent context entry to this batch.
    /// Called during value completion when a batch field is encountered.
    /// </summary>
    public void AddEntry(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData)
    {
        _entries.Add(new BatchEntry(parent, selection, resultValue, scopedContextData));
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Faulted();
                return;
            }

            // 1. Create middleware contexts for each entry.
            var contexts = CreateContexts();

            // 2. Execute the batch pipeline once with all contexts.
            await _pipeline(contexts).ConfigureAwait(false);

            // 3. Complete values synchronously for each context, collecting child tasks.
            CompleteValues(contexts, cancellationToken);

            // 4. Register all child tasks at once.
            if (_taskBuffer.Count > 0)
            {
                _operationContext.Scheduler.Register(
                    CollectionsMarshal.AsSpan(_taskBuffer));
            }
        }
        catch
        {
            Faulted();
        }
    }

    private ImmutableArray<IMiddlewareContext> CreateContexts()
    {
        var builder = ImmutableArray.CreateBuilder<IMiddlewareContext>(_entries.Count);

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var context = new MiddlewareContext();

            context.Initialize(
                entry.Parent,
                entry.Selection,
                entry.ResultValue,
                _operationContext,
                deferUsage: null,
                entry.ScopedContextData);

            builder.Add(context);
        }

        return builder.MoveToImmutable();
    }

    private void CompleteValues(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < contexts.Length; i++)
        {
            var middlewareContext = (MiddlewareContext)contexts[i];
            var entry = _entries[i];
            var result = middlewareContext.Result;

            try
            {
                var completionContext = new ValueCompletionContext(
                    _operationContext,
                    middlewareContext,
                    _taskBuffer,
                    _branchId);

                Complete(completionContext, entry.Selection, entry.ResultValue, result);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _operationContext.ReportError(ex, middlewareContext, entry.Selection, entry.ResultValue.Path);
                }
            }

            if (entry.ResultValue is { IsNullable: false, IsNullOrInvalidated: true })
            {
                PropagateNullValues(entry.ResultValue);
                _operationContext.Result.AddNonNullViolation(entry.ResultValue.Path);
            }
        }
    }

    /// <summary>
    /// Resets the batch task for reuse.
    /// </summary>
    internal new void Reset()
    {
        base.Reset();
        _entries.Clear();
        _taskBuffer.Clear();
        _operationContext = null!;
        _pipeline = null!;
        _selection = null!;
        SelectionPath = null!;
        _branchId = 0;
    }

    /// <summary>
    /// Represents a single entry in the batch — one parent object and its result location.
    /// </summary>
    private readonly record struct BatchEntry(
        object? Parent,
        Selection Selection,
        ResultElement ResultValue,
        IImmutableDictionary<string, object?> ScopedContextData);
}
