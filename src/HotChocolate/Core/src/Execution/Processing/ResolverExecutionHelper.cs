using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ResolverExecutionHelper
    {
        public static async Task ExecuteTasksAsync(
            IOperationContext operationContext)
        {
            if (operationContext.Execution.TaskBacklog.IsEmpty)
            {
                return;
            }

            var proposedTaskCount = operationContext.Operation.ProposedTaskCount;

            if (proposedTaskCount == 1)
            {
                await ExecuteResolversAsync(
                    operationContext.Execution,
                    HandleError,
                    operationContext.RequestAborted);
            }
            else
            {
                var tasks = new Task[proposedTaskCount];

                for (var i = 0; i < proposedTaskCount; i++)
                {
                    tasks[i] = ExecuteResolversAsync(
                        operationContext.Execution,
                        HandleError,
                        operationContext.RequestAborted);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            void HandleError(Exception exception)
            {
                IError error =
                    operationContext.ErrorHandler
                        .CreateUnexpectedError(exception)
                        .SetCode(ErrorCodes.Execution.TaskProcessingError)
                        .Build();

                error = operationContext.ErrorHandler.Handle(error);

                // TODO : this error needs to be reported!
                operationContext.Result.AddError(error);
            }
        }

        private static async Task ExecuteResolversAsync(
            IExecutionContext executionContext,
            Action<Exception> handleError,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested &&
                !executionContext.IsCompleted)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested &&
                        executionContext.TaskBacklog.TryTake(out IAsyncExecutionTask? task))
                    {
                        Task.Factory.StartNew(
                            async () => await task.ExecuteAsync(cancellationToken).ConfigureAwait(false),
                            cancellationToken,
                            TaskCreationOptions.None,
                            executionContext.Scheduler);
                    }

                    await executionContext.TaskBacklog
                        .WaitForTaskAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        handleError(ex);
                    }
                }
            }
        }
    }
}
