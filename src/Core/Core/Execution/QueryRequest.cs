using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public class QueryRequest
    {
        public QueryRequest(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException(
                    "The query cannot be null or empty.",
                    nameof(query));
            }
            Query = query;
        }

        public QueryRequest(string query, string operationName)
            : this(query)
        {
            OperationName = operationName;
        }

        public QueryRequest(IReadOnlyQueryRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Query = request.Query;
            OperationName = request.OperationName;
            VariableValues = request.VariableValues;
            InitialValue = request.InitialValue;
            Properties = request.Properties;
            Services = request.Services;
        }

        public string Query { get; }

        public string OperationName { get; set; }

        public IReadOnlyDictionary<string, object> VariableValues { get; set; }

        public object InitialValue { get; set; }

        public IReadOnlyDictionary<string, object> Properties { get; set; }

        public IServiceProvider Services { get; set; }

        public IReadOnlyQueryRequest ToReadOnly()
        {
            return new ReadOnlyQueryRequest(this);
        }
    }
}
