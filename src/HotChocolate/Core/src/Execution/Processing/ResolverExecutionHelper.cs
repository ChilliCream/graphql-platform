using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ResolverExecutionHelper
    {
        public static Task ExecuteTasksAsync(
            IOperationContext operationContext)
        {
            if (operationContext.Execution.TaskBacklog.IsEmpty)
            {
                return Task.CompletedTask;
            }

            var taskContext = new ExecutionTaskProcessor(operationContext);
            return taskContext.ExecuteAsync();
        }

        private sealed class ExecutionTaskProcessor
        {
            private readonly IOperationContext _context;
            private int _tasks;

            public ExecutionTaskProcessor(IOperationContext context)
            {
                _context = context;
            }

            public Task ExecuteAsync()
            {
                return ExecuteMainProcessorAsync(_context.Execution, _context.RequestAborted);
            }

            private async Task ExecuteMainProcessorAsync(
                IExecutionContext executionContext,
                CancellationToken cancellationToken)
            {
                do
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested &&
                            executionContext.TaskBacklog.TryTake(out IExecutionTask? task))
                        {
                            task.BeginExecute(cancellationToken);
                        }

                        await executionContext.TaskBacklog
                            .WaitForWorkAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            HandleError(ex);
                        }
                    }
                }
                while (!cancellationToken.IsCancellationRequested &&
                    executionContext.IsCompleted);
            }

            private void HandleError(Exception exception)
            {
                IError error =
                    _context.ErrorHandler
                        .CreateUnexpectedError(exception)
                        .SetCode(ErrorCodes.Execution.TaskProcessingError)
                        .Build();

                error = _context.ErrorHandler.Handle(error);

                // TODO : this error needs to be reported!
                _context.Result.AddError(error);
            }
        }
    }
}
