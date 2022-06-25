using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Execution;

internal sealed class RemoteRequestScheduler : IRemoteRequestScheduler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly List<BufferedRequest> _bufferedRequests = new();
    private readonly IBatchScheduler _batchScheduler;
    private bool _taskRegistered;

    public RemoteRequestScheduler(
        IBatchScheduler batchScheduler,
        IRequestExecutor executor)
    {
        _batchScheduler = batchScheduler ??
            throw new ArgumentNullException(nameof(batchScheduler));
        Executor = executor ??
            throw new ArgumentNullException(nameof(executor));
    }

    /// <inheritdoc />
    public ISchema Schema => Executor.Schema;

    /// <inheritdoc />
    public IRequestExecutor Executor { get; }

    /// <inheritdoc />
    public Task<IExecutionResult> ScheduleAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var bufferRequest = new BufferedRequest(request);

        _semaphore.Wait(cancellationToken);

        try
        {
            _bufferedRequests.Add(bufferRequest);

            if (!_taskRegistered)
            {
                _batchScheduler.Schedule(() => ExecuteRequestsInternal(cancellationToken));
                _taskRegistered = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return bufferRequest.Promise.Task;
    }

    private async ValueTask ExecuteRequestsInternal(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_bufferedRequests.Count == 1)
            {
                await ExecuteSingleRequestAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_bufferedRequests.Count > 1)
            {
                await ExecuteBufferedRequestBatchAsync(cancellationToken).ConfigureAwait(false);
            }

            // reset the states so that we are ready for new requests to be buffered.
            _taskRegistered = false;
            _bufferedRequests.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async ValueTask ExecuteSingleRequestAsync(
        CancellationToken cancellationToken)
    {
        BufferedRequest request = _bufferedRequests[0];

        IExecutionResult result = await Executor
            .ExecuteAsync(request.Request, cancellationToken)
            .ConfigureAwait(false);

        if (result is IQueryResult queryResult)
        {
            request.Promise.SetResult(queryResult);
        }
        else
        {
            // since we only support query/mutation at this point we will just fail
            // in the event that something else was returned.
            request.Promise.SetException(new NotSupportedException(
                "Only IQueryResult is supported when batching."));
        }
    }

    private async ValueTask ExecuteBufferedRequestBatchAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var batch = new IQueryRequest[_bufferedRequests.Count];

            for (var j = 0; j < _bufferedRequests.Count; j++)
            {
                batch[j] = _bufferedRequests[j].Request;
            }

            IResponseStream batchQueryResult =
                await Executor.ExecuteBatchAsync(batch, cancellationToken).ConfigureAwait(false);

            var i = 0;
            await foreach (IQueryResult queryResult in batchQueryResult.ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                _bufferedRequests[i++].Promise.SetResult(queryResult);
            }
        }
        catch (Exception ex)
        {
            foreach (BufferedRequest request in _bufferedRequests)
            {
                request.Promise.TrySetException(ex);
            }
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();

        if (Executor is IDisposable d)
        {
            d.Dispose();
        }
    }
}
