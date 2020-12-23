using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using StrawberryShake.Properties;
using static StrawberryShake.Properties.Resources;

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

        IReadOnlyDictionary<string, object?> ContextData { get; }

        Type ResultType { get; }

        object ResultInfo { get; }
    }

    public class OperationResult<T> : IOperationResult<T> where T : class
    {
        public OperationResult(
            T? data,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IReadOnlyDictionary<string, object?>? contextData = null)
        {
            if (data is null && errors is null)
            {
                throw new ArgumentNullException(nameof(data), Response_BodyAndExceptionAreNull);
            }

            Data = data;
            Errors = errors ?? Array.Empty<IError>();
            Extensions = extensions ?? ImmutableDictionary<string, object?>.Empty;
            ContextData = contextData ?? ImmutableDictionary<string, object?>.Empty;
        }

        public T? Data { get; }

        object? IOperationResult.Data => Data;

        public IReadOnlyList<IError> Errors { get; }

        public IReadOnlyDictionary<string, object?> Extensions { get; }

        public IReadOnlyDictionary<string, object?> ContextData { get; }

        public Type ResultType => typeof(T);

        public object ResultInfo { get; }
    }
}
