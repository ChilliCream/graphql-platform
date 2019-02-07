using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public class RemoteQueryClient
        : IRemoteQueryClient
    {
        private readonly object _sync = new object();
        private readonly RemoteRequestDispatcher _dispatcher;
        private List<BufferedRequest> _bufferedRequests =
            new List<BufferedRequest>();
        private int _bufferSize;

        public event RequestBufferedEventHandler BufferedRequest;

        public RemoteQueryClient(
            IServiceProvider services,
            IQueryExecutor executor)
        {
            _dispatcher = new RemoteRequestDispatcher(services, executor);
        }

        public int BufferSize => _bufferSize;

        public Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request)
        {
            var bufferRequest = new BufferedRequest(request);

            lock (_sync)
            {
                _bufferedRequests.Add(bufferRequest);
                _bufferSize++;
                RaiseBufferedRequest();
            }

            return bufferRequest.Promise.Task;
        }

        public Task DispatchAsync(CancellationToken cancellationToken)
        {
            return _dispatcher.DispatchAsync(GetBuffer(), cancellationToken);
        }

        private IList<BufferedRequest> GetBuffer()
        {
            lock (_sync)
            {
                _bufferSize = 0;
                List<BufferedRequest> buffer = _bufferedRequests;
                _bufferedRequests = new List<BufferedRequest>();
                return buffer;
            }
        }

        private void RaiseBufferedRequest()
        {
            if (BufferedRequest != null)
            {
                BufferedRequest.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
