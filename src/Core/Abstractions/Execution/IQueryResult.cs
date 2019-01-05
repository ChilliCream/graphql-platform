using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryResult
    {
        IDictionary<string, object> Data { get; }

        IDictionary<string, object> Extensions { get; }

        ICollection<IError> Errors { get; }
    }
}
