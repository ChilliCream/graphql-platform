using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Requests
{
    internal sealed class RemoteRequestExecutor
        : IRemoteRequestExecutor
        , IDisposable
    {
        private static readonly HashSet<string> _empty = new HashSet<string>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly List<BufferedRequest> _bufferedRequests = new List<BufferedRequest>();
        private readonly IBatchScheduler _batchScheduler;
        private readonly IRequestExecutor _executor;
        private readonly IRequestContextAccessor _requestContextAccessor;
        private bool _taskRegistered;

        public RemoteRequestExecutor(
            IBatchScheduler batchScheduler,
            IRequestExecutor executor,
            IRequestContextAccessor requestContextAccessor)
        {
            _batchScheduler = batchScheduler ??
                throw new ArgumentNullException(nameof(batchScheduler));
            _executor = executor ??
                throw new ArgumentNullException(nameof(executor));
            _requestContextAccessor = requestContextAccessor ??
                throw new ArgumentNullException(nameof(requestContextAccessor));
        }

        /// <iniheritdoc />
        public ISchema Schema => _executor.Schema;

        /// <iniheritdoc />
        public IServiceProvider Services => _executor.Services;

        /// <iniheritdoc />
        public Task<IExecutionResult> ExecuteAsync(
            IQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            var bufferRequest = BufferedRequest.Create(request, Schema);

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

            IExecutionResult result = await _executor
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
            // first we take all buffered requests and merge them into a single request.
            // we however have to group requests by operation type. This means we should
            // end up with one or two requests (query and mutation).
            foreach ((IQueryRequest Merged, IEnumerable<BufferedRequest> Requests) batch in
                MergeRequestHelper.MergeRequests(_bufferedRequests, GetRequestVariableNames()))
            {
                // now we take this merged request and run it against the executor.
                IExecutionResult result = await _executor
                    .ExecuteAsync(batch.Merged, cancellationToken)
                    .ConfigureAwait(false);

                if (result is IQueryResult queryResult)
                {
                    // last we will extract the results for the original buffered requests
                    // and fulfil the promises.
                    MergeRequestHelper.DispatchResults(queryResult, batch.Requests);
                }
                else
                {
                    // since we only support query/mutation at this point we will just fail
                    // in the event that something else was returned.
                    foreach (BufferedRequest request in batch.Requests)
                    {
                        request.Promise.SetException(new NotSupportedException(
                            "Only IQueryResult is supported when batching."));
                    }
                }
            }
        }

        private ISet<string> GetRequestVariableNames()
        {
            IRequestContext context = _requestContextAccessor.RequestContext;

            if (context.ContextData is ConcurrentDictionary<string, object?> optimized)
            {
                return (ISet<string>)optimized.GetOrAdd(RequestVarNames, key =>
                {
                    if (context.Request.VariableValues is { Count: > 0 } variables)
                    {
                        return new HashSet<string>(variables.Keys);
                    }
                    return _empty;
                });
            }

            if(!context.ContextData.TryGetValue(RequestVarNames, out object? value))
            {
                value = context.Request.VariableValues is { Count: > 0 } variables
                    ? new HashSet<string>(variables.Keys)
                    : _empty;
            }

            return (ISet<string>)value;
        }

        public void Dispose()
        {
            _semaphore.Dispose();

            if (_executor is IDisposable d)
            {
                d.Dispose();
            }
        }
    }
}
