using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IExecutionResult
    {
        IReadOnlyList<IError>? Errors { get; }

        IReadOnlyDictionary<string, object?>? Extensions { get; }

        IReadOnlyDictionary<string, object?>? ContextData { get; }
    }
}
