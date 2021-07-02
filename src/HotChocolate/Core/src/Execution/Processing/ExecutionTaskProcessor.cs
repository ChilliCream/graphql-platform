using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

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
                await ProcessTasksAsync(
                    _context.Execution.Work,
                    _context.Execution.BatchDispatcher,
                    buffer,
                    _context.RequestAborted)
                    .ConfigureAwait(false);
            }
            finally
            {
                _context.Execution.TaskBuffers.Return(buffer);
            }
        }

        private async Task ProcessTasksAsync(
            IWorkBacklog backlog,
            IBatchDispatcher batchDispatcher,
            IExecutionTask?[] buffer,
            CancellationToken cancellationToken)
        {
            RESTART:
            try
            {
                do
                {
                    var work = backlog.TryTake(buffer);

                    if (work is 0)
                    {
                        break;
                    }

                    if (buffer[0]!.IsSerial)
                    {
                        try
                        {
                            batchDispatcher.DispatchOnSchedule = true;

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
                            batchDispatcher.DispatchOnSchedule = false;
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
            // Note: we always trigger this method, even if the request was canceled.
            if (!backlog.TryCompleteProcessor())
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
