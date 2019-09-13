using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryResult
        : IReadOnlyQueryResult
    {
        new IDictionary<string, object> Data { get; }

        new IDictionary<string, object> Extensions { get; }

        new ICollection<IError> Errors { get; }

        new IDictionary<string, object> ContextData { get; }

        IReadOnlyQueryResult AsReadOnly();
    }
}
