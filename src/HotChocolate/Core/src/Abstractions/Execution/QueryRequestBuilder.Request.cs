using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public partial class QueryRequestBuilder
    {
        private class QueryRequest
            : IReadOnlyQueryRequest
        {
            public IQuery Query { get; set; }

            public string QueryId { get; set; }

            public string QueryHash { get; set; }

            public string OperationName { get; set; }

            public IVariableValues VariableValues { get; set; }

            public object InitialValue { get; set; }

            public IReadOnlyDictionary<string, object> ContextData { get; set; }

            public IServiceProvider Services { get; set; }

            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }
    }
}
