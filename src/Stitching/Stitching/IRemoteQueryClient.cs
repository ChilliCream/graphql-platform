using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public delegate void RequestBufferedEventHandler(
        IRemoteQueryClient sender,
        EventArgs eventArgs);

    public interface IRemoteQueryClient
    {
        event RequestBufferedEventHandler BufferedRequest;


        int BufferSize { get; }

        Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request);

        Task DispatchAsync(CancellationToken cancellationToken);
    }

    public interface IRemoteQueryRequest
        : IReadOnlyQueryRequest
    {
        new DocumentNode Query { get; }
    }

    internal class RemoteQueryRequest
        : IRemoteQueryRequest
    {
        public RemoteQueryRequest() { }

        public DocumentNode Query { get; internal set; }

        public string OperationName { get; internal set; }

        public IReadOnlyDictionary<string, object> VariableValues
        { get; internal set; }

        public object InitialValue { get; internal set; }

        public IReadOnlyDictionary<string, object> Properties
        { get; internal set; }

        public IServiceProvider Services { get; internal set; }

        string IReadOnlyQueryRequest.Query =>
            QuerySyntaxSerializer.Serialize(Query);
    }

    public interface IRemoteQueryRequestBuilder
    {
        IRemoteQueryRequestBuilder SetQuery(
            DocumentNode query);
        IRemoteQueryRequestBuilder SetOperation(
            string operationName);
        IRemoteQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues);
        IRemoteQueryRequestBuilder AddVariableValue(
            string name, object value);
        IRemoteQueryRequestBuilder SetInitialValue(
            object initialValue);
        IRemoteQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties);
        IRemoteQueryRequestBuilder AddProperties(
            string name, object value);
        IRemoteQueryRequestBuilder SetServices(
            IServiceProvider services);
        IRemoteQueryRequest Create();
    }

    public class RemoteQueryRequestBuilder
        : IRemoteQueryRequestBuilder
    {
        private DocumentNode _query;
        private string _operationName;
        private IDictionary<string, object> _variableValues;
        private object _initialValue;
        private IDictionary<string, object> _properties;
        private IServiceProvider _services;

        public IRemoteQueryRequestBuilder SetQuery(DocumentNode query)
        {
            throw new NotImplementedException();
        }

        public IRemoteQueryRequestBuilder SetOperation(string operationName)
        {
            throw new NotImplementedException();
        }

        public IRemoteQueryRequestBuilder SetVariableValues(IDictionary<string, object> variableValues)
        {
            throw new NotImplementedException();
        }

        public IRemoteQueryRequestBuilder AddVariableValue(string name, object value)
        {
            throw new NotImplementedException();
        }

        public IRemoteQueryRequestBuilder SetInitialValue(object initialValue)
        {
            throw new NotImplementedException();
        }

        public IRemoteQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties)
        {
            _properties = properties;
            return this;
        }

        public IRemoteQueryRequestBuilder AddProperties(
            string name, object value)
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, object>();
            }

            _properties.Add(name, value);
            return this;
        }

        public IRemoteQueryRequestBuilder SetServices(
            IServiceProvider services)
        {
            _services = services;
            return this;
        }

        public IRemoteQueryRequest Create()
        {
            if (_query == null)
            {
                // TODO : Resources
                throw new InvalidOperationException("TODO");
            }

            ValidateOperation(_query, _operationName);

            return new RemoteQueryRequest
            {
                Query = _query,
                OperationName = _operationName,
                InitialValue = _initialValue,
                Services = _services,
                VariableValues = _variableValues == null
                    ? null
                    : new Dictionary<string, object>(_variableValues),
                Properties = _properties == null
                    ? null
                    : new Dictionary<string, object>(_properties)
            };
        }

        private static void ValidateOperation(
            DocumentNode query,
            string operationName)
        {
            var operations = query.Definitions
                .OfType<OperationDefinitionNode>()
                .ToList();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Count == 1)
                {
                    return;
                }

                // TODO : Resources
                throw new InvalidOperationException(
                    "Only queries that contain one operation can be executed " +
                    "without specifying the opartion name.");
            }
            else
            {
                OperationDefinitionNode operation =
                    operations.SingleOrDefault(t =>
                        t.Name.Value.Equals(operationName,
                            StringComparison.Ordinal));
                if (operation == null)
                {
                    // TODO : Resources
                    throw new InvalidOperationException(
                        $"The specified operation `{operationName}` " +
                        "does not exist.");
                }
            }
        }

        public static RemoteQueryRequestBuilder New() => new RemoteQueryRequestBuilder();
    }
}
