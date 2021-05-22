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

            return new ExecutionTaskProcessor(operationContext).ExecuteMainProcessorAsync();
        }
        private async Task ExecuteMainProcessorAsync()
        {
            IExecutionContext executionContext = _context.Execution;
            CancellationToken cancellationToken = _context.RequestAborted;
            IExecutionTask?[] buffer = executionContext.TaskBuffers.Get();

            do
            {
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
                                    await task.WaitForCompletionAsync(cancellationToken);
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

            executionContext.TaskBuffers.Return(buffer);
        }

        private async Task ExecuteSecondaryProcessorAsync()
        {
            IExecutionContext executionContext = _context.Execution;
            CancellationToken cancellationToken = _context.RequestAborted;

            await Task.Yield();

            IExecutionTask?[] buffer = executionContext.TaskBuffers.Get();

            RESTART:
            try
            {
                do
                {
                    var work = executionContext.Work.TryTake(buffer, false);

                    if (work == 0)
                    {
                        break;
                    }

                    for (var i = 0; i < work; i++)
                    {
                        buffer[i]!.BeginExecute(cancellationToken);
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

            if (!executionContext.Work.TryCompleteProcessor())
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
            ExecuteSecondaryProcessorAsync();
#pragma warning restore 4014
        }
    }
}
