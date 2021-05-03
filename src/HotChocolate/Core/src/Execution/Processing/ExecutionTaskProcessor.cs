using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ExecutionTaskProcessor
    {
        private ExecutionTaskProcessor(IOperationContext context)
        {
            context.Execution.TaskBacklog.BackPressureLimitExceeded +=
                OnBackPressureLimitExceeded;

            void OnBackPressureLimitExceeded(object? o, EventArgs eventArgs)
            {
#pragma warning disable 4014
                ExecuteSecondaryProcessorAsync(context.Execution, context.RequestAborted);
#pragma warning restore 4014
            }
        }

        public static Task ExecuteTasksAsync(IOperationContext operationContext)
        {
            if (operationContext.Execution.TaskBacklog.IsEmpty)
            {
                return Task.CompletedTask;
            }

            return new ExecutionTaskProcessor(operationContext)
                .ExecuteMainProcessorAsync(
                    operationContext.Execution,
                    operationContext.RequestAborted);
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


            _isComplete = true;

            if (_processors > 0)
            {

            }
        }

        private async Task ExecuteSecondaryProcessorAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _processors);

            await Task.Yield();
            bool completed;
            RESTART:

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
                Interlocked.Decrement(ref _processors);
                completed = executionContext.TaskBacklog.ProcessorCompleted();
            }

            if (!completed)
            {
                goto RESTART;
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
