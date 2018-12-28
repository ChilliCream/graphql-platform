using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryResponse
    {
        ICollection<KeyValuePair<string, object>> Data { get; }
        IDictionary<string, object> Extensions { get; }
        ICollection<IError> Errors { get; }
    }
}
