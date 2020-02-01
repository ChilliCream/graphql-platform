using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IExecutionResult
    {
        IReadOnlyCollection<IError> Errors { get; }

        IReadOnlyDictionary<string, object> Extensions { get; }

        IReadOnlyDictionary<string, object> ContextData { get; }
    }
}
