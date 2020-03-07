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

            public string QueryName { get; set; }

            public string QueryHash { get; set; }

            public string OperationName { get; set; }

            public IReadOnlyDictionary<string, object> VariableValues { get; set; }

            public object InitialValue { get; set; }

            public IReadOnlyDictionary<string, object> Properties { get; set; }

            public IServiceProvider Services { get; set; }

            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }
    }
}
