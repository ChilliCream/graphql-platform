using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Internal;

namespace HotChocolate.Execution.Processing.Tasks;

internal sealed partial class ResolverTask
{
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (DiagnosticEvents.ResolveFieldValue(_resolverContext))
            {
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
                        _operationContext.Scheduler.Register(_taskBuffer);
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
            _resolverContext.Result = null;
        }
        finally
        {
            _operationContext.Scheduler.Complete(this);

            if (_resolverContext.HasCleanupTasks)
            {
                await _resolverContext.ExecuteCleanupTasksAsync().ConfigureAwait(false);
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
            if (Selection.Arguments.IsFinalNoErrors)
            {
                _resolverContext.Arguments = Selection.Arguments;
                await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            // if this field has arguments that contain variables we first need to coerce them
            // before we can start executing the resolver.
            if (Selection.Arguments.TryCoerceArguments(_resolverContext, out var coercedArgs))
            {
                _resolverContext.Arguments = coercedArgs;
                await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // If cancellation has not been requested for the request we assume this to
                // be a GraphQL resolver error and report it as such.
                // This will let the error handler produce a GraphQL error and
                // we set the result to null.
                ResolverContext.ReportError(ex);
                ResolverContext.Result = null;
            }
        }

        return false;
    }

    private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
    {
        await _resolverContext.ResolverPipeline!(_resolverContext).ConfigureAwait(false);

        var result = _resolverContext.Result;

        if (result is null)
        {
            return;
        }

        if (result is IError error)
        {
            _resolverContext.ReportError(error);
            _resolverContext.Result = null;
            return;
        }

        // if we are not a list we do not need any further result processing.
        if (!_selection.IsList)
        {
            return;
        }

        if (_selection.HasStreamDirective(_operationContext.IncludeFlags))
        {
            _resolverContext.Result = await CreateStreamResultAsync(result).ConfigureAwait(false);
            return;
        }

        if (_selection.HasStreamResult)
        {
            _resolverContext.Result = await CreateListFromStreamAsync(result).ConfigureAwait(false);
            return;
        }

        switch (_resolverContext.Result)
        {
            case IExecutable executable:
                _resolverContext.Result = await executable
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                break;

            case IQueryable queryable:
                _resolverContext.Result = await Task.Run(() =>
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
                }, cancellationToken);
                break;
        }
    }

    private async ValueTask<List<object?>> CreateStreamResultAsync(object result)
    {
        var stream = StreamHelper.CreateStream(result);
        var streamDirective = _selection.GetStreamDirective(_resolverContext.Variables)!;
        var enumerator = stream.GetAsyncEnumerator(_resolverContext.RequestAborted);
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
                _operationContext.DeferredScheduler.Register(
                    new DeferredStream(
                        Selection,
                        streamDirective.Label,
                        _resolverContext.Path.Clone(),
                        _resolverContext.Parent<object>(),
                        count - 1,
                        enumerator,
                        _resolverContext.ScopedContextData));
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
            .WithCancellation(_resolverContext.RequestAborted)
            .ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }


    public void CompleteUnsafe()
    {
        if (!this.IsCompleted())
        {
            Status = _completionStatus;
            _operationContext.Scheduler.Complete(this);
            _objectPool.Return(this);
        }
    }
}
