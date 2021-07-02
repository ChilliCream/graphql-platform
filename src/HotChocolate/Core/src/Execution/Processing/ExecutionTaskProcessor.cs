using System;
using System.Threading;
using System.Threading.Tasks;

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

            BeginExecute(operationContext);

            return operationContext.Execution.Work.Completion;
        }

        private static void BeginExecute(IOperationContext operationContext) =>
            Task.Run(() => new ExecutionTaskProcessor(operationContext).ExecuteProcessorAsync());

        private async Task ExecuteProcessorAsync()
        {
            // we want to immediately yield control back to the caller.
            await Task.Yield();

            IExecutionTask?[] buffer = _context.Execution.TaskBuffers.Get();

            try
            {
                await ProcessTasksAsync(_context.Execution, buffer, _context.RequestAborted)
                    .ConfigureAwait(false);
            }
            finally
            {
                _context.Execution.TaskBuffers.Return(buffer);
            }
        }

        private async Task ProcessTasksAsync(
            IExecutionContext executionContext,
            IExecutionTask?[] buffer,
            CancellationToken cancellationToken)
        {
            RESTART:
            try
            {
                do
                {
                    var work = executionContext.Work.TryTake(buffer);

                    if (work is 0)
                    {
                        break;
                    }

                    if (buffer[0]!.IsSerial)
                    {
                        try
                        {
                            executionContext.BatchDispatcher.DispatchOnSchedule = true;

                            for (var i = 0; i < work; i++)
                            {
                                IExecutionTask task = buffer[i]!;
                                task.BeginExecute(cancellationToken);
                                await task.WaitForCompletionAsync(cancellationToken)
                                    .ConfigureAwait(false);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            executionContext.BatchDispatcher.DispatchOnSchedule = false;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < work; i++)
                        {
                            buffer[i]!.BeginExecute(cancellationToken);
                        }
                    }
                } while (!cancellationToken.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    HandleError(ex);
                }
            }

            // if there is no more work we will try to scale down.
            if (!cancellationToken.IsCancellationRequested &&
                !executionContext.Work.TryCompleteProcessor())
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
            _context.Result.AddError(error);
        }

        private void ScaleProcessors(object? sender, EventArgs eventArgs)
        {
#pragma warning disable 4014
            ExecuteProcessorAsync();
#pragma warning restore 4014
        }
    }
}
