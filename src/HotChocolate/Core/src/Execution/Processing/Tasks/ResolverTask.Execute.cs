using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Internal;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (DiagnosticEvents.ResolveFieldValue(_context))
            {
                // we initialize the field here so we are able to propagate non-null violations
                // through the result tree.
                _context.ParentResult.InitValueUnsafe(_context.ResponseIndex, _context.Selection);
                
                var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                CompleteValue(success, cancellationToken);

                switch (_taskBuffer.Count)
                {
                    case 0:
                        break;

                    case 1:
                        _operationContext.Scheduler.Register(_taskBuffer[0]);
                        break;

                    default:
#if NET6_0_OR_GREATER
                        _operationContext.Scheduler.Register(
                            CollectionsMarshal.AsSpan(_taskBuffer));
#else
                        _operationContext.Scheduler.Register(_taskBuffer);
#endif
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
            // destroyed and we would cause further exceptions.

            // The exception on this level is most likely caused by a cancellation of the request.
            Status = ExecutionTaskStatus.Faulted;
            _context.Result = null;
        }
        finally
        {
            _operationContext.Scheduler.Complete(this);

            if (_context.HasCleanupTasks)
            {
                await _context.ExecuteCleanupTasksAsync().ConfigureAwait(false);
            }

            _objectPool.Return(this);
        }
    }

    private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // We will pre-check if the request was already canceled and mark the task as faulted if
            // this is the case. This essentially gives us a cheap and easy way out without any
            // exceptions.
            if (cancellationToken.IsCancellationRequested)
            {
                _completionStatus = ExecutionTaskStatus.Faulted;
                return false;
            }

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
                foreach (var argument in Selection.Arguments)
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
                // This will let the error handler produce a GraphQL error and
                // we set the result to null.
                Context.ReportError(ex);
                Context.Result = null;
            }
        }

        return false;
    }

    private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
    {
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

        // if we are not a list we do not need any further result processing.
        if (!_selection.IsList)
        {
            return;
        }

        if (_selection.HasStreamDirective(_operationContext.IncludeFlags))
        {
            _context.Result = await CreateStreamResultAsync(result).ConfigureAwait(false);
            return;
        }

        if (_selection.HasStreamResult)
        {
            _context.Result = await CreateListFromStreamAsync(result).ConfigureAwait(false);
            return;
        }

        switch (_context.Result)
        {
            case IExecutable executable:
                _context.Result = await executable
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                break;

            case IQueryable queryable:
                _context.Result = await Task.Run(
                    () =>
                    {
                        var items = new List<object?>();

                        foreach (var o in queryable)
                        {
                            items.Add(o);

                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                        }

                        return items;
                    },
                    cancellationToken);
                break;
        }
    }

    private async ValueTask<List<object?>> CreateStreamResultAsync(object result)
    {
        var stream = StreamHelper.CreateStream(result);
        var streamDirective = _selection.GetStreamDirective(_context.Variables)!;
        var enumerator = stream.GetAsyncEnumerator(_context.RequestAborted);
        var next = true;

        try
        {
            var list = new List<object?>();
            var initialCount = streamDirective.InitialCount;
            var count = 0;

            if (initialCount > 0)
            {
                while (next)
                {
                    count++;
                    next = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    list.Add(enumerator.Current);

                    if (count >= initialCount)
                    {
                        break;
                    }
                }
            }

            if (next)
            {
                // if the stream has more items than the initial requested items then we will
                // defer the rest of the stream.
                var taskDispatcher = _operationContext.DeferredScheduler.Register(
                    new DeferredStream(
                        Selection,
                        streamDirective.Label,
                        _context.Path,
                        _context.Parent<object>(),
                        count - 1,
                        enumerator,
                        _context.ScopedContextData),
                    _context.ParentResult);

                taskDispatcher.Dispatch();
            }

            return list;
        }
        finally
        {
            if (!next)
            {
                // if there is no deferred work we will just dispose the enumerator.
                // in the case we have deferred work, the deferred stream handler is
                // responsible of handling the dispose.
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<List<object?>> CreateListFromStreamAsync(object result)
    {
        var enumerable = StreamHelper.CreateStream(result);
        var list = new List<object?>();

        await foreach (var item in enumerable
            .WithCancellation(_context.RequestAborted)
            .ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
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
            _operationContext.Scheduler.Complete(this);
            _objectPool.Return(this);
        }
    }
}
