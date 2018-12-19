using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public sealed class ReadOnlyQueryRequest
        : IReadOnlyQueryRequest
    {
        public ReadOnlyQueryRequest(QueryRequest queryRequest)
        {
            if (queryRequest == null)
            {
                throw new ArgumentNullException(nameof(queryRequest));
            }

            Query = queryRequest.Query;
            OperationName = queryRequest.OperationName;
            VariableValues = queryRequest.VariableValues;
            InitialValue = queryRequest.InitialValue;
            Properties = queryRequest.Properties;
            Services = queryRequest.Services;
        }

        public string Query { get; }
        public string OperationName { get; }
        public IReadOnlyDictionary<string, object> VariableValues { get; }
        public object InitialValue { get; }
        public IReadOnlyDictionary<string, object> Properties { get; }
        public IServiceProvider Services { get; }
    }
}
