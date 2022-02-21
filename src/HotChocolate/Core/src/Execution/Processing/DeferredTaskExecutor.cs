using System;
using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Execution.Processing;

internal class DeferredTaskExecutor : IAsyncEnumerable<IQueryResult>
{
    private readonly IOperationContextOwner _operationContextOwner;

    public DeferredTaskExecutor(IOperationContextOwner operationContextOwner)
    {
        _operationContextOwner = operationContextOwner ??
            throw new ArgumentNullException(nameof(operationContextOwner));
    }

    public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IOperationContext context = _operationContextOwner.OperationContext;

            while (context.Scheduler.DeferredWork.TryTake(
                out IDeferredExecutionTask? deferredTask))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // we ensure that the previous results are cleared.
                context.ClearResult();

                // next we execute the deferred task.
                IQueryResult? result;
                using (context.DiagnosticEvents.ExecuteDeferredTask())
                {
                    result = await deferredTask.ExecuteAsync(context).ConfigureAwait(false);
                }

                // if we get a result we will yield it to the consumer of the result stream.
                if (result is not null)
                {
                    yield return result;
                }

                // if null is returned and there are no more deferred tasks we will
                // yield a termination result which signals to the consumer that
                // no more results will follow.
                else if (!context.Scheduler.DeferredWork.HasWork)
                {
                    yield return context.ClearResult().TrySetNext(true).BuildResult();
                }
            }
        }
        finally
        {
            _operationContextOwner.Dispose();
        }
    }
}
