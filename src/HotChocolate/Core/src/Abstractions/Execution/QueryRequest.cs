using System;
using System.Collections.Generic;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution
{
    public class QueryRequest : IReadOnlyQueryRequest
    {
        public QueryRequest(
            IQuery? query = null,
            string? queryId = null,
            string? queryHash = null,
            string? operationName = null,
            IReadOnlyDictionary<string, object?>? variableValues = null,
            IReadOnlyDictionary<string, object?>? contextData = null,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IServiceProvider? services = null,
            object? initialValue = null)
        {
            if (query is null && queryId is null)
            {
                throw new QueryRequestBuilderException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNull);
            }

            Query = query;
            QueryId = queryId;
            QueryHash = queryHash;
            OperationName = operationName;
            VariableValues = variableValues;
            ContextData = contextData;
            Extensions = extensions;
            Services = services;
            InitialValue = initialValue;
        }

        public IQuery? Query { get; }

        public string? QueryId { get; }

        public string? QueryHash { get; }

        public string? OperationName { get; }

        public IReadOnlyDictionary<string, object?>? VariableValues { get; }

        public IReadOnlyDictionary<string, object?>? Extensions { get; }

        public IReadOnlyDictionary<string, object?>? ContextData { get; }

        public IServiceProvider? Services { get; }

        public object? InitialValue { get; }
    }
}
