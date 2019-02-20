using System;
using System.Collections.Generic;
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

    public class RemoteQueryRequest
        : IRemoteQueryRequest
    {
        public DocumentNode Query =>

        public string OperationName => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object> VariableValues => throw new NotImplementedException();

        public object InitialValue => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object> Properties => throw new NotImplementedException();

        public IServiceProvider Services => throw new NotImplementedException();

        string IReadOnlyQueryRequest.Query => QuerySyntaxSerializer.Serialize(Query);
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


        public IRemoteQueryRequestBuilder SetProperties(IDictionary<string, object> properties)
        {
            throw new NotImplementedException();
        }


        public IRemoteQueryRequestBuilder AddProperties(string name, object value)
        {
            if(_properties == null)
            { }
        }

        public IRemoteQueryRequestBuilder SetServices(IServiceProvider services)
        {
            _services = services;
            return this;
        }


        public IRemoteQueryRequest Create()
        {
            throw new NotImplementedException();
        }

      

     
       

       
       

        public static RemoteQueryRequestBuilder New() => new RemoteQueryRequestBuilder();

    }
}
