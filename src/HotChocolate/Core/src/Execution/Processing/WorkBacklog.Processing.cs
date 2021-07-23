using System;
using System.Buffers;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkBacklog
    {
#pragma warning disable 4014
        private void StartProcessing() => Task.Run(ExecuteProcessorAsync);
#pragma warning restore 4014

        private async Task ExecuteProcessorAsync()
            => await ProcessTasksAsync().ConfigureAwait(false);

        private async Task ProcessTasksAsync()
        {
            IExecutionTask?[] buffer = ArrayPool<IExecutionTask?>.Shared.Rent(16);

RESTART:
            try
            {
                do
                {
                    var work = TryTake(buffer);

                    if (work != 0)
                    {
                        if (!buffer[0]!.IsSerial)
                        {
                            for (var i = 0; i < work; i++)
                            {
                                buffer[i]!.BeginExecute(_requestAborted);
                                buffer[i] = null;
                            }
                        }
                        else
                        {
                            try
                            {
                                _batchDispatcher.DispatchOnSchedule = true;

                                for (var i = 0; i < work; i++)
                                {
                                    IExecutionTask task = buffer[i]!;
                                    task.BeginExecute(_requestAborted);
                                    await task.WaitForCompletionAsync(_requestAborted)
                                        .ConfigureAwait(false);
                                    buffer[i] = null;

                                    if (_requestAborted.IsCancellationRequested)
                                    {
                                        break;
                                    }
                                }
                            }
                            finally
                            {
                                _batchDispatcher.DispatchOnSchedule = false;
                            }
                        }
                    }
                    else
                    {
                        if (_work.HasRunningTasks || _serial.HasRunningTasks)
                        {
                            await Task.Yield();
                        }

                        break;
                    }

                } while (!_requestAborted.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                if (!_requestAborted.IsCancellationRequested)
                {
                    HandleError(ex);
                }
            }

            // if there is no more work we will try to scale down.
            // Note: we always trigger this method, even if the request was canceled.
            if (await TryStopProcessing() == false)
            {
                goto RESTART;
            }

            buffer.AsSpan().Clear();
            ArrayPool<IExecutionTask?>.Shared.Return(buffer);
        }

        private void HandleError(Exception exception)
        {
            IError error =
                _errorHandler
                    .CreateUnexpectedError(exception)
                    .SetCode(ErrorCodes.Execution.TaskProcessingError)
                    .Build();

            error = _errorHandler.Handle(error);
            _result.AddError(error);
        }
    }
}
