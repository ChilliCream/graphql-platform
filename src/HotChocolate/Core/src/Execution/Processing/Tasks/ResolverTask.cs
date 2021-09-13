using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Tasks
{
    internal sealed class ResolverTask : ResolverTaskBase
    {
        private readonly ObjectPool<ResolverTask> _objectPool;
        private Task? _task;

        public ResolverTask(ObjectPool<ResolverTask> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        public override ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

        public override void BeginExecute(CancellationToken cancellationToken)
        {
            _task = ExecuteAsync(cancellationToken);
        }

        public override Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            _task ?? Task.CompletedTask;

        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (DiagnosticEvents.ResolveFieldValue(ResolverContext))
                {
                    var success = await TryExecuteAsync(cancellationToken).ConfigureAwait(false);
                    CompleteValue(success, cancellationToken);
                }
            }
            catch
            {
                // we suppress any exception if the cancellation was requested.
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                IsCompleted = true;
                OperationContext.Scheduler.Complete(this);
                _objectPool.Return(this);
            }
        }

        private async ValueTask<bool> TryExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (Selection.Arguments.IsFinalNoErrors)
                {
                    ResolverContext.Arguments = Selection.Arguments;
                    await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }

                if (Selection.Arguments.TryCoerceArguments(
                    ResolverContext,
                    out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs))
                {
                    ResolverContext.Arguments = coercedArgs;
                    await ExecuteResolverPipelineAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    ResolverContext.ReportError(ex);
                    ResolverContext.Result = null;
                }
            }

            return false;
        }

        private async ValueTask ExecuteResolverPipelineAsync(CancellationToken cancellationToken)
        {
            await ResolverContext.ResolverPipeline!(ResolverContext).ConfigureAwait(false);

            if (Selection.IsStreamable &&
                ResolverContext.Result is not null &&
                ResolverContext.Result is not IError &&
                ResolverContext.Result is not IEnumerable<IError>)
            {
                IAsyncEnumerable<object?> enumerable =
                    Selection.CreateStream(ResolverContext.Result);
                ResolverContext.Result = CreateStreamResultAsync(enumerable);
            }
            else
            {
                switch (ResolverContext.Result)
                {
                    case IExecutable executable:
                        ResolverContext.Result = await executable
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case IQueryable queryable:
                        ResolverContext.Result = await Task.Run(() =>
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

                    case IError error:
                        ResolverContext.ReportError(error);
                        ResolverContext.Result = null;
                        break;

                    case IEnumerable<IError> errors:
                        foreach (IError error in errors)
                        {
                            ResolverContext.ReportError(error);
                        }

                        ResolverContext.Result = null;
                        break;
                }
            }
        }

        private async ValueTask<List<object?>> CreateStreamResultAsync(
            IAsyncEnumerable<object?> enumerable)
        {
            IAsyncEnumerator<object?>? enumerator = enumerable.GetAsyncEnumerator();
            var next = false;

            try
            {
                next = await enumerator.MoveNextAsync().ConfigureAwait(false);
                var list = new List<object?>();
                var initialCount = Selection.Stream.InitialCount;
                var count = 0;

                if (initialCount > 0)
                {
                    while (next)
                    {
                        count++;
                        list.Add(enumerator.Current);
                        next = await enumerator.MoveNextAsync().ConfigureAwait(false);

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
                    OperationContext.Scheduler.DeferredWork.Register(
                        new DeferredStream(
                            Selection,
                            Selection.Stream.Label,
                            ResolverContext.Path,
                            ResolverContext.Parent<object>(),
                            count - 1, enumerator,
                            ResolverContext.ScopedContextData));
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
    }
}
