using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Options;
using HotChocolate.Types;
using System.Runtime.InteropServices;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    private async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (DiagnosticEvents.ResolveFieldValue(_context))
            {
                var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                CompleteValue(success, cancellationToken);

                if (_stream is not null)
                {
                    await StartStreamAsync().ConfigureAwait(false);
                }

                switch (_taskBuffer.Count)
                {
                    case 0:
                        break;

                    case 1:
                        _operationContext.Scheduler.Register(_taskBuffer[0]);
                        break;

                    default:
                        _operationContext.Scheduler.Register(
                            CollectionsMarshal.AsSpan(_taskBuffer));
                        break;
                }

                if (_groupScope is not null)
                {
                    _childTasksRegistered = _taskBuffer.Count > 0;
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
            _context.Result = null;
        }
        finally
        {
            try
            {
                if (_stream is not null)
                {
                    await DiscardStreamAsync().ConfigureAwait(false);
                }

                if (_context.HasCleanupTasks)
                {
                    await _context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (_groupScope is not null)
                {
                    EndGroupTurn();
                }

                CompleteOnScheduler();
                objectPool.Return(this);
            }
        }
    }

    private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
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
            // If the arguments are already parsed and processed we can just process.
            // Arguments need no pre-processing if they have no variables.
            if (Selection.Arguments.IsFullyCoercedNoErrors)
            {
                _context.Arguments = Selection.Arguments;
                await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            // if we have errors on the compiled execution plan we will report the errors and
            // signal that this resolver task has errors and shall end.
            if (Selection.Arguments.HasErrors)
            {
                foreach (var argument in Selection.Arguments.ArgumentValues)
                {
                    if (argument.HasError)
                    {
                        _context.ReportError(argument.Error!);
                    }
                }

                return false;
            }

            // if this field has arguments that contain variables we first need to coerce them
            // before we can start executing the resolver.
            // We coerce on the args dictionary that is pooled together with this task.
            Selection.Arguments.CoerceArguments(_context.Variables, _args);
            _context.Arguments = _args;
            await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
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
                Context.ReportError(ex);
                Context.Result = null;
            }
        }

        return false;
    }

    private void EndGroupTurn()
    {
        var scope = _groupScope!;
        var childTasksRegistered = _childTasksRegistered;
        _childTasksRegistered = false;

        if (_groupSlot >= 0)
        {
            scope.Release(_groupSlot);
        }
        else if (!childTasksRegistered)
        {
            // without registered child tasks nothing below this selection can ever start, so its members are skipped.
            scope.ReleaseSlotsBelow(_selection);
        }
    }

    private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
    {
        if (_context.Field.DependencyInjectionScope == DependencyInjectionScope.Resolver)
        {
            var serviceScope = _operationContext.Services.CreateAsyncScope();
            _context.Services = serviceScope.ServiceProvider;
            _context.RegisterForCleanup(serviceScope.DisposeAsync);
            _operationContext.ServiceScopeInitializer.Initialize(
                _context, _context.RequestServices, _context.Services);
        }

        await _context.ResolverPipeline!(_context).ConfigureAwait(false);

        var result = _context.Result;

        if (result is null)
        {
            return;
        }

        if (result is IError error)
        {
            _context.ReportError(error);
            _context.Result = null;
            return;
        }

        if (_selection.Field.ResultPostProcessor is not { } postProcessor)
        {
            return;
        }

        if (_selection.IsStreamable
            && _operationContext.Operation.HasIncrementalParts
            && StreamUsage.TryCreate(_selection, _context.Variables, out var streamUsage)
            && postProcessor.ToStreamResultAsync(result, cancellationToken) is { } stream)
        {
            if (streamUsage.InitialCount < 0)
            {
                _context.ReportError(ErrorHelper.StreamInitialCountNegative(_context.Path));
                _context.Result = null;
                return;
            }

            _context.Result = await ReadInitialStreamItemsAsync(stream, streamUsage, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        _context.Result = await postProcessor.ToCompletionResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<List<object?>> ReadInitialStreamItemsAsync(
        IAsyncEnumerable<object?> stream,
        StreamUsage streamUsage,
        CancellationToken cancellationToken)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IAsyncEnumerator<object?>? enumerator = null;
        var isStreaming = false;

        try
        {
            enumerator = stream.GetAsyncEnumerator(cancellation.Token);

            var items = new List<object?>();
            var hasMoreItems = true;

            while (hasMoreItems && items.Count < streamUsage.InitialCount)
            {
                hasMoreItems = await enumerator.MoveNextAsync().ConfigureAwait(false);

                if (hasMoreItems)
                {
                    items.Add(enumerator.Current);
                }
            }

            if (hasMoreItems)
            {
                var options = _operationContext.Schema.Services.GetRequiredService<RequestExecutorOptions>();

                _stream = new StreamTask(
                    _operationContext.RootContext,
                    _selection,
                    _context.Parent<object?>(),
                    _context.ScopedContextData,
                    DeferUsage,
                    _context.Path,
                    streamUsage.Label,
                    items.Count,
                    options.MaxStreamBatchSize,
                    enumerator,
                    cancellation);
                isStreaming = true;
                _stream.StartProducer();
            }

            return items;
        }
        finally
        {
            if (!isStreaming)
            {
                try
                {
                    if (enumerator is not null)
                    {
                        await enumerator.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    cancellation.Dispose();
                }
            }
        }
    }

    private async ValueTask StartStreamAsync()
    {
        var stream = _stream!;

        // the stream only starts if the initial items made it into the result.
        if (_completionStatus is ExecutionTaskStatus.Completed && !_context.ResultValue.IsNullOrInvalidated)
        {
            _stream = null;
            stream.Announce(BranchId, _context.ResultValue);
            _operationContext.RegisterRequestCleanup(stream.DisposeAsync);
            _taskBuffer.Add(stream);
            return;
        }

        await DiscardStreamAsync().ConfigureAwait(false);
    }

    private async ValueTask DiscardStreamAsync()
    {
        var stream = _stream!;
        _stream = null;
        await stream.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// <para>
    /// In most cases a resolver task is rented and returned to its pool after execution.
    /// The execute method itself will return the task.
    /// </para>
    /// <para>
    /// But there are a couple of edge cases where we rent a dummy task and do not execute it.
    /// In these we do want to return it manually.
    /// </para>
    /// <para>Caution: This method is unsafe and could lead to double returns to the pool.</para>
    /// </summary>
    public async ValueTask CompleteUnsafeAsync()
    {
        if (!this.IsCompleted())
        {
            if (_context.HasCleanupTasks)
            {
                await _context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
            }

            Status = _completionStatus;
            CompleteOnScheduler();
            objectPool.Return(this);
        }
    }
}
