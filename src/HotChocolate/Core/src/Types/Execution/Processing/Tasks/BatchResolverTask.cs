using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
/// An execution task that collects multiple parent contexts for a batch resolver
/// and executes them in a single invocation.
/// </summary>
internal sealed class BatchResolverTask(
    ObjectPool<BatchResolverTask> objectPool,
    ObjectPool<ResolverTask> resolverTaskPool) : ExecutionTask
{
    private readonly List<ResolverTask> _resolverTasks = [];
    private readonly List<BatchEntry> _entries = [];
    private readonly List<IExecutionTask> _taskBuffer = [];
    private OperationContext _operationContext = null!;
    private ObjectField _field = null!;
    private SelectionPath _selectionPath = null!;
    private int _branchId;
    private DeferUsage? _deferUsage;

    /// <inheritdoc />
    public override int BranchId => _branchId;

    /// <inheritdoc />
    public override bool IsDeferred => false;

    /// <inheritdoc />
    protected override IExecutionTaskContext Context => _operationContext;

    /// <summary>
    /// Gets the number of entries currently collected in this batch.
    /// </summary>
    internal int EntryCount => _entries.Count;

    /// <summary>
    /// Gets the selection path this batch task is associated with.
    /// Used by the work scheduler to track active paths.
    /// </summary>
    internal SelectionPath SelectionPath => _selectionPath;

    /// <summary>
    /// Adds a parent context entry to this batch.
    /// Called during value completion when a batch field is encountered.
    /// </summary>
    public void AddEntry(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData)
        => _entries.Add(new BatchEntry(parent, selection, resultValue, scopedContextData));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var contexts = CreateContexts();

        try
        {
            // Execute the batch pipeline once with all contexts.
            await _field.BatchResolverPipeline!(contexts).ConfigureAwait(false);

            // Complete values synchronously for each context, collecting child tasks.
            CompleteValues(contexts, cancellationToken);

            // Register all child tasks at once.
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
        finally
        {
            ReturnResolverTasks();
        }
    }

    /// <inheritdoc />
    protected override ValueTask OnAfterCompletedAsync(CancellationToken cancellationToken)
    {
        objectPool.Return(this);
        return ValueTask.CompletedTask;
    }

    private ImmutableArray<IMiddlewareContext> CreateContexts()
    {
        var builder = ImmutableArray.CreateBuilder<IMiddlewareContext>(_entries.Count);

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var resolverTask =
                _operationContext.CreateResolverTask(
                    entry.Parent,
                    entry.Selection,
                    entry.ResultValue,
                    entry.ScopedContextData,
                    _branchId,
                    _deferUsage);

            _resolverTasks.Add(resolverTask);
            builder.Add(resolverTask.MiddlewareContext);
        }

        return builder.MoveToImmutable();
    }

    private void CompleteValues(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < contexts.Length; i++)
        {
            var middlewareContext = Unsafe.As<MiddlewareContext>(contexts[i]);
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

    private void ReturnResolverTasks()
    {
        foreach (var task in _resolverTasks)
        {
            resolverTaskPool.Return(task);
        }

        _resolverTasks.Clear();
    }

    /// <summary>
    /// Initializes this batch task.
    /// </summary>
    public void Initialize(
        OperationContext operationContext,
        ObjectField field,
        SelectionPath selectionPath,
        int branchId,
        DeferUsage? deferUsage)
    {
        _operationContext = operationContext;
        _field = field;
        _selectionPath = selectionPath;
        _branchId = branchId;
        _deferUsage = deferUsage;
    }

    /// <summary>
    /// Resets the batch task for reuse.
    /// </summary>
    internal new void Reset()
    {
        base.Reset();

        _resolverTasks.Clear();
        _entries.Clear();
        _taskBuffer.Clear();
        _operationContext = null!;
        _field = null!;
        _selectionPath = null!;
        _branchId = 0;
        _deferUsage = null;
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
