using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ExecutionTaskProcessor
    {
        private readonly IOperationContext _context;
        private readonly IWorkBacklog _backlog;
        private readonly ObjectPool<IExecutionTask?[]> _bufferPool;
        private IBatchDispatcher _batchDispatcher = default!;
        private CancellationToken _cancellationToken;

        public ExecutionTaskProcessor(
            IOperationContext context,
            IWorkBacklog backlog,
            ObjectPool<IExecutionTask?[]> bufferPool)
        {
            _context = context;
            _backlog = backlog;
            _bufferPool = bufferPool;
            _backlog.BackPressureLimitExceeded += ScaleProcessors;
        }

        public void Initialize(
            IBatchDispatcher batchDispatcher,
            CancellationToken cancellationToken)
        {
            _batchDispatcher = batchDispatcher;
            _cancellationToken = cancellationToken;
        }

        public void Clean()
        {
            _batchDispatcher = default!;
            _cancellationToken = default;
        }

        private async Task ExecuteProcessorAsync()
        {
            IExecutionTask?[] buffer = _bufferPool.Get();

            try
            {
                await ProcessTasksAsync(_backlog, _batchDispatcher, buffer, _cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _bufferPool.Return(buffer);
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
            Task.Run(ExecuteProcessorAsync, _cancellationToken);
#pragma warning restore 4014
        }
    }
}
