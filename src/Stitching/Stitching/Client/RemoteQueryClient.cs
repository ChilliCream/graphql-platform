using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Client
{
    public class RemoteQueryClient
        : IRemoteQueryClient
    {
        private readonly object _sync = new object();
        private readonly RemoteRequestDispatcher _dispatcher;
        private List<BufferedRequest> _bufferedRequests = new List<BufferedRequest>();
        private int _bufferSize;

        public event RequestBufferedEventHandler BufferedRequest;

        public RemoteQueryClient(
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
            if (BufferedRequest != null)
            {
                BufferedRequest.Invoke(this, EventArgs.Empty);
            }
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
