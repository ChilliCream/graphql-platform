using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution
{
    internal class DeferredTaskExecutor : IAsyncEnumerable<IQueryResult>
    {
        private readonly IOperationContextOwner _operationContextOwner;

        public DeferredTaskExecutor(IOperationContextOwner operationContextOwner)
        {
            _operationContextOwner = operationContextOwner ??
                throw new ArgumentNullException(nameof(operationContextOwner));
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            try
            {
                IOperationContext context = _operationContextOwner.OperationContext;

                while (context.Execution.DeferredTaskBacklog.TryTake(
                    out IDeferredExecutionTask? deferredTask))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    context.Result.Reset();

                    if (!context.Execution.DeferredTaskBacklog.IsEmpty)
                    {
                        context.Result.SetExtension("hasNext", true);
                    }

                    yield return await deferredTask.ExecuteAsync(context).ConfigureAwait(false);
                }
            }
            finally
            {
                _operationContextOwner.Dispose();
            }
        }
    }
}
