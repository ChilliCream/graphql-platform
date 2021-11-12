using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

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
            }

            Status = _completionStatus;
        }
        catch (OperationCanceledException)
        {
            // If we run into this exception the request was aborted.
            // In this case we do nothing and just return.
            Status = ExecutionTaskStatus.Faulted;
            _resolverContext.Result = null;
        }
        catch (Exception ex)
        {
            Status = ExecutionTaskStatus.Faulted;
            _resolverContext.Result = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                _resolverContext.ReportError(ex);
            }
        }

        _operationContext.Scheduler.Complete(this);
        _objectPool.Return(this);
    }

    private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _completionStatus = ExecutionTaskStatus.Faulted;
            return false;
        }

        if (Selection.Arguments.IsFinalNoErrors)
        {
            _resolverContext.Arguments = Selection.Arguments;
            await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (Selection.Arguments.TryCoerceArguments(
            _resolverContext,
            out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
        {
            _resolverContext.Arguments = coercedArgs;
            await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
    {
        await _resolverContext.ResolverPipeline!(_resolverContext).ConfigureAwait(false);

        if (_resolverContext.Result is null)
        {
            return;
        }

        if (_resolverContext.Result is IError error)
        {
            _resolverContext.ReportError(error);
            _resolverContext.Result = null;
            return;
        }

        // if we are not a list we do not need any further result processing.
        if (!Selection.IsList)
        {
            return;
        }

        if (Selection.IsStreamable)
        {
            StreamDirective streamDirective =
                Selection.SyntaxNode.Directives.GetStreamDirective(
                    _resolverContext.Variables)!;
            if (streamDirective.If)
            {
                _resolverContext.Result =
                    await CreateStreamResultAsync(streamDirective)
                        .ConfigureAwait(false);
                return;
            }
        }

        if (Selection.MaybeStream)
        {
            _resolverContext.Result =
                await CreateListFromStreamAsync()
                    .ConfigureAwait(false);
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

    private async ValueTask<List<object?>> CreateStreamResultAsync(
        StreamDirective streamDirective)
    {
        IAsyncEnumerable<object?> enumerable = Selection.CreateStream(_resolverContext.Result!);
        IAsyncEnumerator<object?> enumerator = enumerable.GetAsyncEnumerator();
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
                _operationContext.Scheduler.DeferredWork.Register(
                    new DeferredStream(
                        Selection,
                        streamDirective.Label,
                        _resolverContext.Path,
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

    private async ValueTask<List<object?>> CreateListFromStreamAsync()
    {
        IAsyncEnumerable<object?> enumerable = Selection.CreateStream(_tesolverContext.Result!);
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
