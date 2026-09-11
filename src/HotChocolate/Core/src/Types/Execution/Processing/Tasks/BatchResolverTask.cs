using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.ValueCompletion;

namespace HotChocolate.Execution.Processing.Tasks;

/// <summary>
/// An execution task that collects multiple parent contexts for a batch resolver
/// and executes them in a single invocation.
/// </summary>
internal sealed class BatchResolverTask : IResolverTask
{
    private readonly List<ResolverTask> _resolverTasks = [];
    private readonly List<BatchEntry> _entries = [];
    private readonly List<IExecutionTask> _taskBuffer = [];
    private readonly List<Dictionary<string, ArgumentValue>> _rentedArgs = [];
    private readonly HashSet<IMiddlewareContext> _excluded = [];
    private readonly ObjectPool<BatchResolverTask> _objectPool;
    private readonly ObjectPool<ResolverTask> _resolverTaskPool;
    private readonly ObjectPool<Dictionary<string, ArgumentValue>> _argumentMapPool;
    private readonly HashSet<int> _branchIds = [];
    private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
    private WorkScheduler _scheduler = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private ObjectField _field = null!;
    private SelectionPath _selectionPath = null!;
    private int _branchId;

    public BatchResolverTask(
        ObjectPool<BatchResolverTask> objectPool,
        ObjectPool<ResolverTask> resolverTaskPool,
        ObjectPool<Dictionary<string, ArgumentValue>> argumentMapPool)
    {
        _objectPool = objectPool;
        _resolverTaskPool = resolverTaskPool;
        _argumentMapPool = argumentMapPool;
    }

    /// <summary>
    /// Gets or sets the internal execution id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets the execution branch identifier this task belongs to.
    /// </summary>
    public int BranchId => _branchId;

    /// <summary>
    /// Gets all branch identifiers that are associated with this task.
    /// </summary>
    public IReadOnlySet<int> BranchIds => _branchIds;

    /// <summary>
    /// Gets the primary defer usage for this batch.
    /// </summary>
    internal DeferUsage? DeferUsage { get; private set; }

    /// <inheritdoc />
    public IExecutionTaskContext Context => _entries[0].OperationContext;

    /// <summary>
    /// Gets the selection path this batch task is associated with.
    /// Used by the work scheduler to track active paths.
    /// </summary>
    public SelectionPath FieldSelectionPath => _selectionPath;

    /// <inheritdoc />
    public ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

    /// <inheritdoc />
    public ExecutionTaskStatus Status { get; private set; }

    /// <inheritdoc />
    public IExecutionTask? Next { get; set; }

    /// <inheritdoc />
    public IExecutionTask? Previous { get; set; }

    /// <inheritdoc />
    public object? State { get; set; }

    /// <inheritdoc />
    public bool IsSerial { get; set; }

    /// <inheritdoc />
    public bool IsRegistered { get; set; }

    /// <inheritdoc />
    public bool IsDeferred => DeferUsage is not null;

    /// <inheritdoc />
    public void BeginExecute(CancellationToken cancellationToken)
    {
#pragma warning disable CA2012
        Status = ExecutionTaskStatus.Running;
        _ = ExecuteAsync(cancellationToken);
#pragma warning restore CA2012
    }

    /// <summary>
    /// Adds a parent context entry to this batch.
    /// Called during value completion when a batch field is encountered.
    /// </summary>
    internal bool AddEntry(
        OperationContext operationContext,
        object? parent,
        Selection selection,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData,
        int branchId)
    {
        _entries.Add(new BatchEntry(operationContext, parent, selection, resultValue, scopedContextData, branchId));
        return _branchIds.Add(branchId);
    }

    private async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var contexts = CreateContexts();

        try
        {
            using (_diagnosticEvents.ResolveFieldValue(contexts[0]))
            {
                var success = await TryExecuteAsync(contexts, cancellationToken).ConfigureAwait(false);
                CompleteValues(success, contexts, cancellationToken);

                switch (_taskBuffer.Count)
                {
                    case 0:
                        break;

                    case 1:
                        _scheduler.Register(_taskBuffer[0]);
                        break;

                    default:
                        _scheduler.Register(
                            CollectionsMarshal.AsSpan(_taskBuffer));
                        break;
                }
            }

            Status = _completionStatus;
        }
        catch
        {
            // If an exception occurs on this level it means that something was wrong with the
            // operation context.

            // In this case we will mark the task as faulted and set the result to null.

            // However, we will not report or rethrow the exception since the context was already
            // destroyed, and we would cause further exceptions.

            // The exception on this level is most likely caused by a cancellation of the request.
            Status = ExecutionTaskStatus.Faulted;
        }
        finally
        {
            try
            {
                for (var i = 0; i < contexts.Length; i++)
                {
                    var context = Unsafe.As<MiddlewareContext>(contexts[i]);
                    if (context.HasCleanupTasks)
                    {
                        try
                        {
                            await context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            Status = ExecutionTaskStatus.Faulted;
                        }
                    }
                }
            }
            finally
            {
                _scheduler.Complete(this);
                ReturnResolverTasks();
                _objectPool.Return(this);
            }
        }
    }

    private async ValueTask<bool> TryExecuteAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        // We will pre-check if the request was already canceled and mark the task as faulted if
        // this is the case. This essentially gives us a cheap and easy way out without any
        // exceptions.
        if (cancellationToken.IsCancellationRequested)
        {
            _completionStatus = ExecutionTaskStatus.Faulted;
            return false;
        }

        try
        {
            ImmutableArray<IMiddlewareContext>.Builder? survivors = null;

            for (var i = 0; i < contexts.Length; i++)
            {
                var context = Unsafe.As<MiddlewareContext>(contexts[i]);

                if (TryCoerceArguments(_entries[i].OperationContext, context, cancellationToken))
                {
                    survivors?.Add(context);
                    continue;
                }

                survivors ??= CollectSurvivors(contexts, i);
                _excluded.Add(context);
                context.Result = null;
                CompleteValue(_entries[i].OperationContext, context, success: false, cancellationToken);
            }

            var remaining = survivors?.ToImmutable() ?? contexts;
            if (remaining.IsDefaultOrEmpty)
            {
                return true;
            }

            await ExecuteBatchPipelineAsync(remaining, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // If cancellation has not been requested for the request we assume this to
                // be a GraphQL resolver error and report it as such.
                // This will let the error handler produce a GraphQL error, and
                // we set the result to null.
                for (var i = 0; i < contexts.Length; i++)
                {
                    var context = Unsafe.As<MiddlewareContext>(contexts[i]);

                    // Excluded contexts must not receive a batch-wide error. A swept context's
                    // slot is erased so it gets no error, and a set-aside context already carries
                    // its own error from the isolated partitioner failure.
                    if (_excluded.Contains(context))
                    {
                        continue;
                    }

                    if (!context.HasErrors)
                    {
                        context.ReportError(ex);
                        context.Result = null;
                    }
                }
            }
        }

        return false;
    }

    private bool TryCoerceArguments(
        OperationContext operationContext,
        MiddlewareContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var arguments = context.Selection.Arguments;
            if (arguments.IsFullyCoercedNoErrors)
            {
                context.Arguments = arguments;
                return true;
            }

            if (arguments.HasErrors)
            {
                foreach (var argument in arguments.ArgumentValues)
                {
                    if (argument.HasError)
                    {
                        context.ReportError(argument.Error!);
                    }
                }

                return false;
            }

            var args = _argumentMapPool.Get();
            _rentedArgs.Add(args);
            context.Arguments = args;

            // Runtime argument values are coerced before dispatch and retained for resolver access.
            foreach (var definition in arguments.ArgumentValues)
            {
                if (definition.IsFullyCoerced)
                {
                    args.Add(definition.Name, definition);
                    continue;
                }

                var literal = VariableRewriter.Rewrite(
                    definition.ValueLiteral!,
                    definition.Type,
                    definition.DefaultValue,
                    context.Variables);

                object? value;
                try
                {
                    value = operationContext.InputParser.ParseLiteral(literal, definition, typeof(object));
                }
                catch (LeafCoercionException ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var error = ex.Errors[0].WithPath(context.Path);
                        if (error.Locations is not { Count: > 0 }
                            && definition.ValueLiteral?.Location is { } location)
                        {
                            error = error.WithLocations([new Location(location.Line, location.Column)]);
                        }

                        context.ReportError(error);
                    }

                    return false;
                }

                if (value is IOptional optional)
                {
                    value = optional.Value;
                }

                args.Add(
                    definition.Name,
                    new ArgumentValue(
                        definition,
                        literal.TryGetValueKind(out var kind) ? kind : ValueKind.Unknown,
                        true,
                        definition.IsDefaultValue,
                        value,
                        literal));
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                context.ReportError(ex);
            }

            return false;
        }
    }

    private async ValueTask ExecuteBatchPipelineAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        // Create DI scopes for each context if needed.
        if (_field.DependencyInjectionScope == DependencyInjectionScope.Resolver)
        {
            // we only use a single service scope for all contexts
            // as they all run in the same resolver.
            var first = Unsafe.As<MiddlewareContext>(contexts[0]);
            var serviceScope = first.RequestServices.CreateAsyncScope();
            first.Services = serviceScope.ServiceProvider;
            first.RegisterForCleanup(serviceScope.DisposeAsync);
            var entryIndex = 0;
            while (!ReferenceEquals(_resolverTasks[entryIndex].Context, first))
            {
                entryIndex++;
            }

            _entries[entryIndex].OperationContext.ServiceScopeInitializer.Initialize(
                first, first.RequestServices, first.Services);

            for (var i = 1; i < contexts.Length; i++)
            {
                var context = Unsafe.As<MiddlewareContext>(contexts[i]);
                context.Services = serviceScope.ServiceProvider;
            }
        }

        if (_field.BatchPartitionKeyResolver is { } partitioner && contexts.Length > 1)
        {
            await ExecutePartitionedBatchPipelineAsync(contexts, partitioner, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await DispatchAsync(contexts, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ExecutePartitionedBatchPipelineAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        BatchPartitionKeyResolver partitioner,
        CancellationToken cancellationToken)
    {
        // A throwing partitioner must not poison its sibling contexts. The partitioner is invoked
        // exactly once per context. A throw is isolated to that single context (eagerly completed
        // and set aside) while the surviving contexts keep their computed key for grouping. Eager
        // completion is safe here because no partition group has executed yet.
        var keys = ArrayPool<ulong>.Shared.Rent(contexts.Length);

        try
        {
            ImmutableArray<IMiddlewareContext>.Builder? survivors = null;
            ulong firstKey = 0;
            var firstSurvivorIndex = -1;
            var entryIndex = 0;

            for (var i = 0; i < contexts.Length; i++)
            {
                // Survivors retain entry order even when argument failures compact the array.
                while (!ReferenceEquals(_resolverTasks[entryIndex].Context, contexts[i]))
                {
                    entryIndex++;
                }

                var key = TryPartition(partitioner, _entries[entryIndex++].OperationContext, contexts[i], cancellationToken, out var faulted);

                if (faulted)
                {
                    // The faulted context is set aside, it never enters any partition or dispatch
                    // but stays in the task lifecycle for the normal end-of-task cleanup.
                    survivors ??= CollectSurvivors(contexts, i);
                    continue;
                }

                if (firstSurvivorIndex < 0)
                {
                    firstSurvivorIndex = i;
                    firstKey = key;
                }

                if (survivors is null)
                {
                    keys[i] = key;
                }
                else
                {
                    keys[survivors.Count] = key;
                    survivors.Add(contexts[i]);
                }
            }

            if (firstSurvivorIndex < 0)
            {
                // Every context faulted in the partitioner, so there is nothing left to dispatch.
                return;
            }

            var remaining = survivors?.ToImmutable() ?? contexts;

            if (remaining.Length == 1)
            {
                await DispatchAsync(remaining, cancellationToken).ConfigureAwait(false);
                return;
            }

            Dictionary<ulong, ImmutableArray<IMiddlewareContext>.Builder>? partitions = null;

            for (var i = 1; i < remaining.Length; i++)
            {
                var key = keys[i];

                if (partitions is null)
                {
                    if (key == firstKey)
                    {
                        continue;
                    }

                    partitions = [];
                    var firstPartition = ImmutableArray.CreateBuilder<IMiddlewareContext>(i);

                    for (var j = 0; j < i; j++)
                    {
                        firstPartition.Add(remaining[j]);
                    }

                    partitions.Add(firstKey, firstPartition);
                }

                ref var partition = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    partitions,
                    key,
                    out var exists);

                if (!exists)
                {
                    partition = ImmutableArray.CreateBuilder<IMiddlewareContext>();
                }

                partition!.Add(remaining[i]);
            }

            if (partitions is null)
            {
                await DispatchAsync(remaining, cancellationToken).ConfigureAwait(false);
                return;
            }

            foreach (var partition in partitions.Values)
            {
                await DispatchAsync(partition.ToImmutable(), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(keys);
        }
    }

    private async ValueTask DispatchAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        // The sweep sorts out contexts whose result slot was already erased by null propagation
        // elsewhere in the operation, so executing them would be dead work.
        var dispatch = SweepInvalidated(contexts);

        if (!dispatch.IsDefaultOrEmpty)
        {
            await ExecuteSingleBatchPipelineAsync(dispatch, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invokes the partitioner for a single context and isolates a throwing partitioner so it
    /// cannot poison its sibling contexts in the same batch. On a throw the context's error is
    /// reported, its result is nulled and it is completed eagerly so the standard null
    /// propagation runs before any partition group is dispatched.
    /// </summary>
    private ulong TryPartition(
        BatchPartitionKeyResolver partitioner,
        OperationContext operationContext,
        IMiddlewareContext context,
        CancellationToken cancellationToken,
        out bool faulted)
    {
        try
        {
            faulted = false;
            return partitioner(context);
        }
        catch (Exception ex)
        {
            faulted = true;

            var middlewareContext = Unsafe.As<MiddlewareContext>(context);

            // The faulted context is set aside, so it must be excluded from the final completion
            // pass to avoid completing it twice.
            _excluded.Add(context);

            if (cancellationToken.IsCancellationRequested)
            {
                // When cancellation is requested we skip reporting, nulling and eager completion
                // entirely. The context is left in its current state for the end-of-task cleanup.
                return 0;
            }

            if (!middlewareContext.HasErrors)
            {
                middlewareContext.ReportError(ex);
                middlewareContext.Result = null;
            }

            CompleteValue(operationContext, middlewareContext, success: true, cancellationToken);
            return 0;
        }
    }

    private static ImmutableArray<IMiddlewareContext>.Builder CollectSurvivors(
        ImmutableArray<IMiddlewareContext> contexts,
        int faultedIndex)
    {
        // The first faulted context switches the path from the original contexts array to an
        // explicit survivor list seeded with every context before the fault. No fault occurred
        // before this index so each survivor key already sits at its survivor index in the key
        // buffer and needs no compaction.
        var builder = ImmutableArray.CreateBuilder<IMiddlewareContext>(contexts.Length - 1);

        for (var i = 0; i < faultedIndex; i++)
        {
            builder.Add(contexts[i]);
        }

        return builder;
    }

    /// <summary>
    /// Sorts out every context whose result slot was already erased by null propagation, either
    /// by an eagerly completed partitioner failure in this batch or by a concurrent task
    /// elsewhere in the operation. A sorted-out context needs no error and no result because its
    /// slot is gone, but it stays in the task lifecycle for cleanup. The check is per context
    /// against its own result document so it is always scoped to the matching request and
    /// variable set even when the batch task merges across variable sets.
    /// </summary>
    private ImmutableArray<IMiddlewareContext> SweepInvalidated(
        ImmutableArray<IMiddlewareContext> contexts)
    {
        // The verdict for each context is snapshotted in a single pass so that a concurrent
        // invalidation between counting and building cannot under-fill the survivor list. A stale
        // not-erased verdict only dispatches dead work, which matches the previous behavior, while
        // a thrown exception from a mismatched count is avoided entirely.
        var erased = ArrayPool<bool>.Shared.Rent(contexts.Length);

        try
        {
            var erasedCount = 0;

            for (var i = 0; i < contexts.Length; i++)
            {
                if (IsParentInvalidated(Unsafe.As<MiddlewareContext>(contexts[i]).ResultValue))
                {
                    erased[i] = true;
                    erasedCount++;
                }
                else
                {
                    erased[i] = false;
                }
            }

            if (erasedCount == 0)
            {
                return contexts;
            }

            // A swept context receives no completion, error or result because its slot is gone, so
            // it must be excluded from the final completion pass.
            for (var i = 0; i < contexts.Length; i++)
            {
                if (erased[i])
                {
                    _excluded.Add(contexts[i]);
                }
            }

            if (erasedCount == contexts.Length)
            {
                return [];
            }

            var builder = ImmutableArray.CreateBuilder<IMiddlewareContext>(contexts.Length - erasedCount);

            for (var i = 0; i < contexts.Length; i++)
            {
                if (!erased[i])
                {
                    builder.Add(contexts[i]);
                }
            }

            return builder.MoveToImmutable();
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(erased);
        }

        static bool IsParentInvalidated(ResultElement value)
        {
            do
            {
                if (value.IsParentNullOrInvalidated)
                {
                    return true;
                }

                value = value.Parent;
            } while (value.ValueKind is not JsonValueKind.Undefined);

            return false;
        }
    }

    private async ValueTask ExecuteSingleBatchPipelineAsync(
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        await _field.BatchResolver!(contexts).ConfigureAwait(false);

        // Post-process results for each context.
        if (_field.ResultPostProcessor is { } postProcessor)
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                var context = Unsafe.As<MiddlewareContext>(contexts[i]);
                var result = context.Result;

                if (result is null)
                {
                    continue;
                }

                if (result is IError error)
                {
                    context.ReportError(error);
                    context.Result = null;
                    continue;
                }

                context.Result = await postProcessor
                    .ToCompletionResultAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                var context = Unsafe.As<MiddlewareContext>(contexts[i]);
                var result = context.Result;

                if (result is IError error)
                {
                    context.ReportError(error);
                    context.Result = null;
                }
            }
        }
    }

    private void CompleteValues(
        bool success,
        ImmutableArray<IMiddlewareContext> contexts,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < contexts.Length; i++)
        {
            var context = contexts[i];

            // A context is excluded when it was either eagerly completed by an isolated
            // partitioner failure or swept because its result slot was already erased. In both
            // cases the final completion pass must leave it untouched so that an eagerly completed
            // context is not completed twice and a swept context receives no spurious result.
            if (_excluded.Contains(context))
            {
                continue;
            }

            CompleteValue(_entries[i].OperationContext, Unsafe.As<MiddlewareContext>(context), success, cancellationToken);
        }
    }

    private void CompleteValue(
        OperationContext operationContext,
        MiddlewareContext context,
        bool success,
        CancellationToken cancellationToken)
    {
        var resultValue = context.ResultValue;
        var result = context.Result;
        var taskCount = _taskBuffer.Count;

        try
        {
            // we will only try to complete the resolver value if there are no known errors.
            if (success)
            {
                var completionContext =
                    new ValueCompletionContext(
                        operationContext,
                        context,
                        _taskBuffer,
                        context.BranchId);

                Complete(completionContext, context.Selection, resultValue, result);
            }
        }
        catch (OperationCanceledException)
        {
            _completionStatus = ExecutionTaskStatus.Faulted;
            context.Result = null;
            return;
        }
        catch (Exception ex)
        {
            context.Result = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                context.ReportError(ex);
                resultValue.SetNullValue();
            }
        }

        if (_taskBuffer.Count > taskCount
            && (resultValue.IsNullOrInvalidated || resultValue.IsParentNullOrInvalidated))
        {
            _taskBuffer.RemoveRange(taskCount, _taskBuffer.Count - taskCount);
        }

        if (resultValue is { IsNullable: false, IsNullOrInvalidated: true })
        {
            if (operationContext.PropagateNullValues)
            {
                PropagateNullValues(resultValue);
            }
            else
            {
                resultValue.SetNullValue();
            }

            operationContext.Result.AddNonNullViolation(context.Path);
        }
    }

    private ImmutableArray<IMiddlewareContext> CreateContexts()
    {
        var builder = ImmutableArray.CreateBuilder<IMiddlewareContext>(_entries.Count);

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var resolverTask =
                entry.OperationContext.CreateResolverTask(
                    entry.Parent,
                    entry.Selection,
                    entry.ResultValue,
                    entry.ScopedContextData,
                    entry.BranchId,
                    DeferUsage);

            var context = Unsafe.As<MiddlewareContext>(resolverTask.Context);
            context.BranchId = entry.BranchId;

            _resolverTasks.Add(resolverTask);
            builder.Add(context);
        }

        return builder.MoveToImmutable();
    }

    private void ReturnResolverTasks()
    {
        foreach (var task in _resolverTasks)
        {
            _resolverTaskPool.Return(task);
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
        _scheduler = operationContext.Scheduler;
        _diagnosticEvents = operationContext.DiagnosticEvents;
        _field = field;
        _selectionPath = selectionPath;
        _branchId = branchId;
        DeferUsage = deferUsage;
    }

    /// <summary>
    /// Resets the batch task for reuse.
    /// </summary>
    internal bool Reset()
    {
        _completionStatus = ExecutionTaskStatus.Completed;
        _resolverTasks.Clear();
        _entries.Clear();
        _taskBuffer.Clear();
        _excluded.Clear();

        foreach (var args in _rentedArgs)
        {
            _argumentMapPool.Return(args);
        }

        _rentedArgs.Clear();
        _branchIds.Clear();
        _scheduler = null!;
        _diagnosticEvents = null!;
        _field = null!;
        _selectionPath = null!;
        _branchId = 0;
        DeferUsage = null;
        Status = ExecutionTaskStatus.WaitingToRun;
        IsSerial = false;
        IsRegistered = false;
        Next = null;
        Previous = null;
        State = null;
        return true;
    }

    /// <summary>
    /// Represents a parent object and its result location in the owning operation context.
    /// </summary>
    private readonly record struct BatchEntry(
        OperationContext OperationContext,
        object? Parent,
        Selection Selection,
        ResultElement ResultValue,
        IImmutableDictionary<string, object?> ScopedContextData,
        int BranchId);
}
