using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal static class ResolverExecutionHelper
    {
        public static Task ExecuteTasksAsync(IOperationContext operationContext)
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
                context.Execution.TaskBacklog.BackPressureLimitExceeded +=
                    OnBackPressureLimitExceeded;

                void OnBackPressureLimitExceeded(object? o, EventArgs eventArgs)
                {
                    var taskCount = _tasks;
                    while (taskCount < 5)
                    {
                        var lastTaskCount =
                            Interlocked.CompareExchange(ref _tasks, taskCount + 1, taskCount);

                        if (taskCount == lastTaskCount)
                        {
#pragma warning disable 4014
                            ExecuteChildProcessorAsync(context.Execution, context.RequestAborted);
#pragma warning restore 4014
                        }
                        else
                        {
                            taskCount = _tasks;
                        }
                    }
                }
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
                } while (!cancellationToken.IsCancellationRequested &&
                    !executionContext.IsCompleted);
            }

            private async Task ExecuteChildProcessorAsync(
                IExecutionContext executionContext,
                CancellationToken cancellationToken)
            {
                await Task.Yield();

                try
                {
                    while (!cancellationToken.IsCancellationRequested &&
                        executionContext.TaskBacklog.TryTake(out IExecutionTask? task))
                    {
                        task.BeginExecute(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        HandleError(ex);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _tasks);
                }
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
