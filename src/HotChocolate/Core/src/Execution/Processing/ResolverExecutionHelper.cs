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
            var proposedTaskCount = operationContext.Operation.ProposedTaskCount;
            var tasks = new Task[proposedTaskCount];

            for (var i = 0; i < proposedTaskCount; i++)
            {
                tasks[i] = StartExecutionTaskAsync(
                    operationContext.Execution,
                    HandleError,
                    operationContext.RequestAborted);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

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

        private static Task StartExecutionTaskAsync(
            IExecutionContext executionContext,
            Action<Exception> handleError,
            CancellationToken cancellationToken) =>
            Task.Run(() => ExecuteResolvers(executionContext, handleError, cancellationToken));

        private static async Task ExecuteResolvers(
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
                        executionContext.TaskBacklog.TryTake(out IExecutionTask? task))
                    {
                        task.BeginExecute(cancellationToken);
                    }

                    await executionContext.TaskBacklog
                        .WaitForTaskAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch(Exception ex)
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
