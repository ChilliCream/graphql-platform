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
            IExecutionContext executionContext = _context.Execution;
            CancellationToken cancellationToken = _context.RequestAborted;
            IExecutionTask?[] buffer = executionContext.TaskBuffers.Get();

            await Task.Yield();

            RESTART:
            try
            {
                do
                {
                    var work = executionContext.Work.TryTake(buffer, true);

                    if (work == 0)
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

            if (!cancellationToken.IsCancellationRequested &&
                !executionContext.Work.TryCompleteProcessor())
            {
                goto RESTART;
            }

            executionContext.TaskBuffers.Return(buffer);
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
