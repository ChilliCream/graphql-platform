using System;
using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Execution.Processing
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
            CancellationToken cancellationToken = default)
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

                    context.Result.Clear();
                    context.Execution.Reset();

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
