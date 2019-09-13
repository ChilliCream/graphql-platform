using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    public interface IHttpQueryRequest
    {
        string OperationName { get; }
        string NamedQuery { get; }
        string Query { get; }
        IReadOnlyDictionary<string, object> Variables { get; }
    }
}
