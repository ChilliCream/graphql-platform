using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using StrawberryShake.Properties;
using StrawberryShake.Remove;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake
{
    public interface IOperationResult<out T> : IOperationResult where T : class
    {
        new T? Data { get; }

        IOperationResultDataFactory<T> DataFactory { get; }
    }

    public interface IOperationResult
    {
        object? Data { get; }

        Type DataType { get; }

        IOperationResultDataInfo? DataInfo { get; }

        IReadOnlyList<IError> Errors { get; }

        IReadOnlyDictionary<string, object?> Extensions { get; }

        IReadOnlyDictionary<string, object?> ContextData { get; }
    }

    public class OperationResult<T> : IOperationResult<T> where T : class
    {
        public OperationResult(
            T? data,
            IOperationResultDataInfo? dataInfo,
            IOperationResultDataFactory<T> dataFactory,
            IReadOnlyList<IError>? errors,
            IReadOnlyDictionary<string, object?>? extensions = null,
            IReadOnlyDictionary<string, object?>? contextData = null)
        {
            if (data is null && errors is null)
            {
                throw new ArgumentNullException(nameof(data), Response_BodyAndExceptionAreNull);
            }

            Data = data;
            DataInfo = dataInfo;
            DataFactory = dataFactory;
            Errors = errors ?? Array.Empty<IError>();
            Extensions = extensions ?? ImmutableDictionary<string, object?>.Empty;
            ContextData = contextData ?? ImmutableDictionary<string, object?>.Empty;
        }

        public T? Data { get; }

        object? IOperationResult.Data => Data;

        public Type DataType => typeof(T);

        public IOperationResultDataInfo? DataInfo { get; }

        public IOperationResultDataFactory<T> DataFactory { get; }

        public IReadOnlyList<IError> Errors { get; }

        public IReadOnlyDictionary<string, object?> Extensions { get; }

        public IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
