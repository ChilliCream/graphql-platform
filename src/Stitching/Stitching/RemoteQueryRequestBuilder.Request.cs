using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public partial class RemoteQueryRequestBuilder
    {
        private class RemoteQueryRequest
            : IRemoteQueryRequest
        {
            public DocumentNode Query { get; set; }

            public string OperationName { get; set; }

            public IReadOnlyDictionary<string, object> VariableValues
            { get; set; }

            public object InitialValue { get; set; }

            public IReadOnlyDictionary<string, object> Properties
            { get; set; }

            public IServiceProvider Services { get; set; }

            string IReadOnlyQueryRequest.Query =>
                QuerySyntaxSerializer.Serialize(Query);
        }
    }
}
