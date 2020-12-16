using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResult<out T> : IOperationResult where T : class
    {
        new T? Data { get; }
    }

    public interface IOperationResult
    {
        object? Data { get; }

        IReadOnlyList<IError> Errors { get; }

        IReadOnlyDictionary<string, object?> Extensions { get; }

        Type ResultType { get; }

        bool HasErrors { get; }

        void EnsureNoErrors();
    }
}
