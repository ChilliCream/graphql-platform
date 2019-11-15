using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public sealed class OperationResult<T>
        : IOperationResult<T>
        where T : class
    {
        private static readonly Dictionary<string, object?> _empty =
            new Dictionary<string, object?>();

        public OperationResult(
            T? data,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extensions)
        {
            Data = data;
            Errors = errors ?? Array.Empty<IError>();
            Extensions = extensions ?? _empty;
        }

        public T? Data { get; }

        public IReadOnlyList<IError> Errors { get; }

        public IReadOnlyDictionary<string, object?> Extensions { get; }

        public bool HasErrors => Errors.Count > 0;

        public Type ResultType => typeof(T);

        object? IOperationResult.Data => Data;

        public void EnsureNoErrors()
        {
            if (Errors.Count > 0)
            {
                throw new GraphQLException(Errors);
            }
        }
    }
}
