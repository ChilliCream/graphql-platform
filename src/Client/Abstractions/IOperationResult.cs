using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResult
    {
        object Data { get; }

        IReadOnlyList<IError> Errors { get; }

        IReadOnlyDictionary<string, object> Extensions { get; }
    }
}
