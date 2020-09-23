using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution
{
    internal sealed class DeferredQueryExecutor
    {
        public IAsyncEnumerable<IQueryResult> ExecuteAsync(
            IOperationContextOwner operationContextOwner) =>
            new DeferredTaskExecutor(operationContextOwner);

        private class DeferredTaskExecutor : IAsyncEnumerable<IQueryResult>
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

                    while (!context.DeferredTasks.IsEmpty)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        IDeferredExecutionTask deferredTask = context.DeferredTasks.Take();

                        context.Result.Reset();

                        if (!context.DeferredTasks.IsEmpty)
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
}
