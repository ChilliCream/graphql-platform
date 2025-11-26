using HotChocolate.Fetching;
using static System.Environment;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler : IObserver<BatchDispatchEventArgs>
{
    private readonly IExecutionTask?[] _buffer = new IExecutionTask?[ProcessorCount * 2];

    /// <summary>
    /// Execute the work.
    /// </summary>
    public async Task ExecuteAsync()
    {
        AssertNotPooled();

        try
        {
            await ExecuteInternalAsync(_buffer);
        }
        finally
        {
            _buffer.AsSpan().Clear();
        }
    }

    private async Task ExecuteInternalAsync(IExecutionTask?[] buffer)
    {
RESTART:
        _diagnosticEvents.StartProcessing(_requestContext);

        try
        {
            do
            {
                var work = TryTake(buffer);

                if (work is not 0)
                {
                    var first = buffer[0]!;
                    if (!first.IsSerial)
                    {
                        // if work is NOT serial we will just enqueue it and not wait
                        // for it to finish.
                        first.BeginExecute(_ct);
                        buffer[0] = null;

                        for (var i = 1; i < work; i++)
                        {
                            buffer[i]!.BeginExecute(_ct);
                            buffer[i] = null;
                        }
                    }
                    else
                    {
                        first.BeginExecute(_ct);
                        await WaitForTask(first.Id).ConfigureAwait(false);
                        buffer[0] = null;
                    }
                }
                else
                {
                    break;
                }
            } while (!_ct.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            if (!_ct.IsCancellationRequested)
            {
                HandleError(ex);
            }
        }

        TryDispatchOrComplete();

        if (await TryPauseAsync().ConfigureAwait(false))
        {
            goto RESTART;
        }

        _ct.ThrowIfCancellationRequested();
    }

    private async Task WaitForTask(uint taskId)
    {
        while (!_completed.ContainsKey(taskId))
        {
            // we are waiting for completion of the current task
            // so we force the `TryDispatchOrComplete` to seek completion
            // even though the _work backlog might still have unprocessed
            // tasks.
            TryDispatchOrComplete(isWaitingForTaskCompletion: true);
            await TryPauseAsync().ConfigureAwait(false);
        }
    }

    private int TryTake(IExecutionTask?[] buffer)
    {
        var size = 0;

        lock (_sync)
        {
            var isParallel = !_work.IsEmpty || _work.HasRunningTasks;
            var work = isParallel ? _work : _serial;

            if (isParallel)
            {
                // The default behavior for tasks is that they can be executed in parallel.
                // We will always try to dequeue multiple tasks at once so that we avoid having
                // many lock interactions.
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (!work.TryTake(out var task))
                    {
                        break;
                    }

                    size++;
                    buffer[i] = task;
                }
            }
            else
            {
                // For serial work we dequeue one task at a time.
                // Parallel work is always preferred, so we take a single serial task and see if
                // this results in more parallel work.
                if (work.TryTake(out var task))
                {
                    size = 1;
                    buffer[0] = task;
                }
            }
        }

        return size;
    }

    private void HandleError(Exception exception)
    {
        var error =
            ErrorBuilder
                .FromException(exception)
                .SetCode(ErrorCodes.Execution.TaskProcessingError)
                .Build();

        error = _errorHandler.Handle(error);

        if (error is AggregateError aggregateError)
        {
            foreach (var innerError in aggregateError.Errors)
            {
                _result.AddError(innerError);
            }
        }
        else
        {
            _result.AddError(error);
        }
    }

    private void TryDispatchOrComplete(bool isWaitingForTaskCompletion = false)
    {
        if (_isCompleted)
        {
            return;
        }

        lock (_sync)
        {
            if (_isCompleted)
            {
                return;
            }

            if (!isWaitingForTaskCompletion)
            {
                isWaitingForTaskCompletion = _work is { HasRunningTasks: true, IsEmpty: true };
            }

            var hasWork = !_work.IsEmpty || !_serial.IsEmpty;

            if (isWaitingForTaskCompletion)
            {
                _signal.Reset();

                if (Interlocked.CompareExchange(ref _hasBatches, 0, 1) == 1)
                {
                    _batchDispatcher.BeginDispatch(_ct);
                }
            }
            else
            {
                if (!hasWork)
                {
                    _isCompleted = true;
                }
            }
        }
    }

    private async ValueTask<bool> TryPauseAsync()
    {
        if (!_isCompleted)
        {
            if (_signal.IsPaused)
            {
                _diagnosticEvents.StopProcessing(_requestContext);
                await _signal;
            }

            return true;
        }

        return false;
    }

    public void OnNext(BatchDispatchEventArgs value)
    {
        if (value.Type is BatchDispatchEventType.Enqueued
            or BatchDispatchEventType.Dispatched
            or BatchDispatchEventType.CoordinatorCompleted)
        {
            Interlocked.Exchange(ref _hasBatches, 1);
            _signal.Set();
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }
}
