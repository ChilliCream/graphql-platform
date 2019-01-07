using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IReadOnlyQueryResult
        : IExecutionResult
    {
        IReadOnlyDictionary<string, object> Data { get; }
    }
}
