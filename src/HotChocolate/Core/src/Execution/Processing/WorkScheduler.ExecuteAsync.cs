using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing;

internal partial class WorkScheduler
{
    private readonly IExecutionTask?[] _buffer = new IExecutionTask?[16];

    public async Task ExecuteAsync()
    {
        _stateMachine.Start();

        if (_suspended.HasWork)
        {
            _suspended.CopyTo(_work, _serial, _stateMachine);
        }

        IExecutionTask?[] buffer = _buffer;

RESTART:
        lock (_sync)
        {
            _processing = true;
        }

        _diagnosticEvents.StartProcessing(_requestContext);

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
        if (await TryStopProcessingAsync().ConfigureAwait(false) == false)
        {
            goto RESTART;
        }

        buffer.AsSpan().Clear();
        _requestAborted.ThrowIfCancellationRequested();
    }

    private void HandleError(Exception exception)
    {
        IError error =
            _errorHandler
                .CreateUnexpectedError(exception)
                .SetCode(ErrorCodes.Execution.TaskProcessingError)
                .Build();

        error = _errorHandler.Handle(error);

        if (error is AggregateError aggregateError)
        {
            foreach (IError? innerError in aggregateError.Errors)
            {
                _result.AddError(innerError);
            }
        }
        else
        {
            _result.AddError(error);
        }
    }
}
