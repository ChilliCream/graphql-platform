using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryResponse
    {
        OrderedDictionary Data { get; }

        IDictionary<string, object> Extensions { get; }

        ICollection<IError> Errors { get; }
    }
}
