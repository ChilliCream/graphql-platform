using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Execution;

internal sealed class ParallelBatchRequestHandler : IRemoteBatchRequestHandler
{
    private readonly IRemoteRequestHandler[] _requestHandlers;

    public ParallelBatchRequestHandler(IEnumerable<IRemoteRequestHandler> requestHandlers)
    {
        _requestHandlers = requestHandlers?.ToArray() ??
            throw new ArgumentNullException(nameof(requestHandlers));
    }

    public Task<IBatchQueryResult> ExecuteAsync(
        IEnumerable<IQueryRequest> requestBatch,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IBatchQueryResult>(
            new BatchQueryResult(
                () => new Executor(_requestHandlers, requestBatch),
                Array.Empty<IError>()));

    private sealed class Executor : IAsyncEnumerable<IQueryResult>
    {
        private readonly IRemoteRequestHandler[] _requestHandlers;
        private readonly IEnumerable<IQueryRequest> _requestBatch;

        public Executor(
            IRemoteRequestHandler[] requestHandlers,
            IEnumerable<IQueryRequest> requestBatch)
        {
            _requestHandlers = requestHandlers;
            _requestBatch = requestBatch;
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<IExecutionResult>>();

            foreach (IQueryRequest request in _requestBatch)
            {
                IRemoteRequestHandler? requestHandler =
                    Array.Find(_requestHandlers, handler => handler.CanHandle(request));

                if (requestHandler is null)
                {
                    throw new NotSupportedException(
                        StitchingResources.RemoteRequestMiddleware_Request_Not_Supported);
                }

                tasks.Add(requestHandler.ExecuteAsync(request, cancellationToken));
            }

            foreach (Task<IExecutionResult> task in tasks)
            {
                if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                {
                    yield return (IQueryResult)task.Result;
                }
                else
                {
                    yield return (IQueryResult)await task.ConfigureAwait(false);
                }
            }

        }
    }
}
