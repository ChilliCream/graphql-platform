using System.Collections.Generic;

namespace HotChocolate.Stitching.Utilities
{
    internal class HttpQueryRequest
        : IHttpQueryRequest
    {
        public string Id { get; set; }
        public string Query { get; set; }
        public string OperationName { get; set; }
        public IReadOnlyDictionary<string, object> Variables { get; set; }
    }
}
