using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Execution.Properties;
using HotChocolate.Fetching;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkBacklog
    {
#pragma warning disable 4014
        private void StartProcessing() => ExecuteProcessorAsync();
#pragma warning restore 4014

        private async Task ExecuteProcessorAsync()
            => await ProcessTasksAsync(_buffer).ConfigureAwait(false);

        private async Task ProcessTasksAsync(IExecutionTask?[] buffer)
        {
            RESTART:
            try
            {
                do
                {
                    var work = TryTake(buffer);

                    if (work is 0)
                    {
                        break;
                    }

                    if (buffer[0]!.IsSerial)
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
                    else
                    {
                        for (var i = 0; i < work; i++)
                        {
                            buffer[i]!.BeginExecute(_requestAborted);
                        }
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
