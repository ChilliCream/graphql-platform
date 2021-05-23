using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using HotChocolate.Execution.Processing.Plan;

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
                QueryPlan rootQueryPlan = context.QueryPlan;

                while (context.Execution.DeferredWork.TryTake(
                    out IDeferredExecutionTask? deferredTask))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    context.Result.Clear();
                    context.Execution.Reset();
                    context.QueryPlan = rootQueryPlan;

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
