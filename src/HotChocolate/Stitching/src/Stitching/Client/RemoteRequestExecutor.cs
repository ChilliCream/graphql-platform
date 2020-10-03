using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Client
{
    public class RemoteRequestExecutor
        : IRemoteRequestExecutor
    {
        private readonly object _sync = new object();
        private readonly RemoteRequestDispatcher _dispatcher;
        private List<BufferedRequest> _bufferedRequests = new List<BufferedRequest>();
        private int _bufferSize;

        public event RequestBufferedEventHandler BufferedRequest;

        public RemoteRequestExecutor(
            IServiceProvider services,
            IQueryExecutor executor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _dispatcher = new RemoteRequestDispatcher(services, executor);
        }

        public IQueryExecutor Executor { get; }

        public int BufferSize => _bufferSize;

        public Task<IExecutionResult> ExecuteAsync(IReadOnlyQueryRequest request)
        {
            var bufferRequest = new BufferedRequest(NormalizeRequest(request));

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
            BufferedRequest?.Invoke(this, EventArgs.Empty);
        }

        private IReadOnlyQueryRequest NormalizeRequest(
            IReadOnlyQueryRequest originalRequest)
        {
            ImmutableDictionary<string, object> normalizedVariables =
                ImmutableDictionary<string, object>.Empty;

            OperationDefinitionNode operation = null;

            foreach (KeyValuePair<string, object> variable in originalRequest.VariableValues)
            {
                if (variable.Value as IValueNode is null)
                {
                    if (operation is null)
                    {
                        operation = ResolveOperationDefinition(
                            originalRequest.Query, originalRequest.OperationName);
                    }

                    IValueNode normalizedValue = RewriteVariable(
                        operation, variable.Key, variable.Value);

                    normalizedVariables =
                        normalizedVariables.SetItem(variable.Key, normalizedValue);
                }
            }

            if (normalizedVariables.Count > 0)
            {
                QueryRequestBuilder builder = QueryRequestBuilder.From(originalRequest);

                foreach (KeyValuePair<string, object> normalized in normalizedVariables)
                {
                    builder.SetVariableValue(normalized.Key, normalized.Value);
                }

                return builder.Create();
            }

            return originalRequest;
        }

        private IValueNode RewriteVariable(
            OperationDefinitionNode operation,
            string name,
            object value)
        {
            VariableDefinitionNode variableDefinition =
                operation.VariableDefinitions.FirstOrDefault(t =>
                    string.Equals(t.Variable.Name.Value, name, StringComparison.Ordinal));

            if (variableDefinition is { }
                && Executor.Schema.TryGetType(
                    variableDefinition.Type.NamedType().Name.Value,
                    out INamedInputType namedType))
            {
                IInputType variableType = (IInputType)variableDefinition.Type.ToType(namedType);
                return variableType.ParseValue(value);
            }

            throw new InvalidOperationException(
                $"The specified variable `{name}` does not exist or is of an " +
                "invalid type.");
        }

        private OperationDefinitionNode ResolveOperationDefinition(
            IQuery query, string operationName)
        {
            DocumentNode documents = Utf8GraphQLParser.Parse(query.ToSpan());

            OperationDefinitionNode operation = operationName is null
                ? documents.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault()
                : documents.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault(t =>
                    string.Equals(t.Name?.Value, operationName, StringComparison.Ordinal));

            if (operation is null)
            {
                throw new InvalidOperationException(
                    "The provided remote query does not contain the specified operation." +
                    Environment.NewLine +
                    Environment.NewLine +
                    $"`{query.ToString()}`");
            }

            return operation;
        }
    }
}

namespace HotChocolate.Stitching
{
    internal class RemoteRequestExecutor
        : IRemoteRequestExecutor
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly List<BufferedRequest> _bufferedRequests = new List<BufferedRequest>();
        private readonly IRequestExecutor _executor;
        private readonly IBatchScheduler _batchScheduler;
        private bool _taskRegistered;

        public RemoteRequestExecutor(IRequestExecutor executor, IBatchScheduler batchScheduler)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _batchScheduler = batchScheduler;
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
            var bufferRequest = new BufferedRequest(NormalizeRequest(request));

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
                // first we take all buffered requests and merge them into
                // a single request in order to reduce network traffic.
                IQueryRequest request = MergeRequests();

                // now we take this merged request and run it against the executor.
                IExecutionResult result = await _executor
                    .ExecuteAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                // last we will extract the results for the original buffered requests
                // and fulfil the promises.
                DistributeResults(result);

                // reset the states so that we are ready for new requests to be buffered.
                _taskRegistered = false;
                _bufferedRequests.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private IQueryRequest NormalizeRequest(IQueryRequest originalRequest)
        {
            throw new NotImplementedException();
        }

        private IQueryRequest MergeRequests()
        {
            throw new NotImplementedException();
        }

        private void DistributeResults(IExecutionResult result)
        {
            throw new NotImplementedException();
        }
    }
}
