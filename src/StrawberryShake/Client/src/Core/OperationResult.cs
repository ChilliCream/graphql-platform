using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake
{
    public class OperationResult<T> : IOperationResult<T> where T : class
    {
        public OperationResult(
            T? data,
            IOperationResultDataInfo? dataInfo,
            IOperationResultDataFactory<T> dataFactory,
            IReadOnlyList<IClientError>? errors,
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
            Errors = errors ?? Array.Empty<IClientError>();
            Extensions = extensions ?? ImmutableDictionary<string, object?>.Empty;
            ContextData = contextData ?? ImmutableDictionary<string, object?>.Empty;
        }

        public T? Data { get; }

        object? IOperationResult.Data => Data;

        public Type DataType => typeof(T);

        public IOperationResultDataInfo? DataInfo { get; }

        public IOperationResultDataFactory<T> DataFactory { get; }

        object IOperationResult.DataFactory => DataFactory;

        public IReadOnlyList<IClientError> Errors { get; }

        public IReadOnlyDictionary<string, object?> Extensions { get; }

        public IReadOnlyDictionary<string, object?> ContextData { get; }

        public IOperationResult<T> WithData(T data, IOperationResultDataInfo dataInfo) =>
            new OperationResult<T>(data, dataInfo, DataFactory, Errors, Extensions, ContextData);
    }
}
