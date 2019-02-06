using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
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
        private bool _isBufferFilled = false;

        public event EventHandler<EventArgs> BufferedRequest;

        public RemoteQueryClient(IQueryExecutor executor)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            _dispatcher = new RemoteRequestDispatcher(executor);
        }

        public bool IsBufferFilled => _isBufferFilled;

        public Task<IExecutionResult> ExecuteAsync(
            IResolverContext context,
            IReadOnlyQueryRequest request)
        {
            var bufferRequest = new BufferedRequest(request);

            lock (_sync)
            {
                _bufferedRequests.Add(bufferRequest);
                _isBufferFilled = true;
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
                _isBufferFilled = false;
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

    internal class BufferedRequest
    {
        public BufferedRequest(IReadOnlyQueryRequest request)
        {
            Request = request;
            Document = Parser.Default.Parse(request.Query);
            Promise = new TaskCompletionSource<IExecutionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public IReadOnlyQueryRequest Request { get; }
        public DocumentNode Document { get; }
        public TaskCompletionSource<IExecutionResult> Promise { get; }
        public IDictionary<string, string> Aliases { get; set; }
    }

    internal class RemoteRequestDispatcher
    {
        private readonly IQueryExecutor _queryExecutor;

        public RemoteRequestDispatcher(IQueryExecutor queryExecutor)
        {

        }

        public Task DispatchAsync(IList<BufferedRequest> requests, CancellationToken cancellationToken)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            var rewriter = new MergeQueryRewriter();
            var variableValues = new Dictionary<string, object>();

            for (int i = 0; i < requests.Count; i++)
            {
                MergeRequest(requests[i], rewriter, variableValues, $"__{i}_");
            }

            return DispatchRequestsAsync(
                requests,
                rewriter.Merge(),
                variableValues,
                cancellationToken);
        }

        private async Task DispatchRequestsAsync(
            IList<BufferedRequest> requests,
            DocumentNode mergedQuery,
            IReadOnlyDictionary<string, object> variableValues,
            CancellationToken cancellationToken)
        {
            var mergedRequest = new QueryRequest(
                QuerySyntaxSerializer.Serialize(mergedQuery))
            {
                VariableValues = variableValues
            };

            var mergedResult = (IReadOnlyQueryResult)await _queryExecutor
                .ExecuteAsync(mergedRequest.ToReadOnly(), cancellationToken);
            var handledErrors = new HashSet<IError>();

            for (int i = 0; i < requests.Count; i++)
            {
                IQueryResult result = ExtractResult(
                    requests[i].Aliases,
                    mergedResult,
                    handledErrors);

                if (handledErrors.Count < mergedResult.Errors.Count
                    && i == requests.Count - 1)
                {
                    foreach (IError error in mergedResult.Errors
                        .Except(handledErrors))
                    {
                        result.Errors.Add(error);
                    }
                }

                requests[i].Promise.SetResult(result);
            }
        }

        private void MergeRequest(
            BufferedRequest bufferedRequest,
            MergeQueryRewriter rewriter,
            IDictionary<string, object> variableValues,
            NameString requestPrefix)
        {
            MergeVariables(
                bufferedRequest.Request.VariableValues,
                variableValues,
                requestPrefix);

            bufferedRequest.Aliases = rewriter.AddQuery(
                bufferedRequest.Document, requestPrefix);
        }

        private void MergeVariables(
            IReadOnlyDictionary<string, object> original,
            IDictionary<string, object> merged,
            NameString requestPrefix)
        {
            foreach (KeyValuePair<string, object> item in original)
            {
                string variableName = MergeUtils.CreateNewName(
                    item.Key, requestPrefix);
                merged.Add(variableName, item.Value);
            }
        }

        private IQueryResult ExtractResult(
            IDictionary<string, string> aliases,
            IReadOnlyQueryResult mergedResult,
            ICollection<IError> handledErrors)
        {
            var result = new QueryResult();

            foreach (KeyValuePair<string, string> alias in aliases)
            {
                if (mergedResult.Data.TryGetValue(alias.Key, out object o))
                {
                    result.Data.Add(alias.Value, o);
                }
            }

            foreach (IError error in mergedResult.Errors)
            {
                if (error.Path != null
                    && error.Path.FirstOrDefault() is string s
                    && aliases.ContainsKey(s))
                {
                    handledErrors.Add(error);
                    result.Errors.Add(error);
                }
            }

            return result;
        }
    }
}
