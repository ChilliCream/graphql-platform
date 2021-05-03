using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ExecutionTaskProcessor
    {
        private readonly IOperationContext _context;

        private ExecutionTaskProcessor(IOperationContext context)
        {
            _context = context;
            context.Execution.Work.BackPressureLimitExceeded += ScaleProcessors;
        }

        public static Task ExecuteAsync(IOperationContext operationContext)
        {
            if (operationContext.Execution.Work.IsEmpty)
            {
                return Task.CompletedTask;
            }

            return new ExecutionTaskProcessor(operationContext).ExecuteMainProcessorAsync();
        }
        private async Task ExecuteMainProcessorAsync()
        {
            IExecutionContext executionContext = _context.Execution;
            CancellationToken cancellationToken = _context.RequestAborted;

            do
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested &&
                        executionContext.Work.TryTake(out IExecutionTask? task))
                    {
                        task.BeginExecute(cancellationToken);
                    }

                    while (!cancellationToken.IsCancellationRequested &&
                        executionContext.Work.TryTakeSerial(out IExecutionTask? task))
                    {
                        task.BeginExecute(cancellationToken);
                        await task.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
                    }

                    await executionContext.Work
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

        private async Task ExecuteSecondaryProcessorAsync()
        {
            IExecutionContext executionContext = _context.Execution;
            CancellationToken cancellationToken = _context.RequestAborted;

            await Task.Yield();
            bool completed;
RESTART:

            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       executionContext.Work.TryTake(out IExecutionTask? task))
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
                completed = executionContext.Work.TryCompleteProcessor();
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

        private void ScaleProcessors(object? sender, EventArgs eventArgs)
        {
#pragma warning disable 4014
            ExecuteSecondaryProcessorAsync();
#pragma warning restore 4014
        }
    }
}
