using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResult<T> : IOperationResult where T : class
    {
        new T? Data { get; }

        new IOperationResultDataFactory<T> DataFactory { get; }

        IOperationResult<T> WithData(T data, IOperationResultDataInfo dataInfo);
    }

    public interface IOperationResult
    {
        object? Data { get; }

        Type DataType { get; }

        IOperationResultDataInfo? DataInfo { get; }

        object DataFactory { get; }

        IReadOnlyList<IClientError> Errors { get; }

        IReadOnlyDictionary<string, object?> Extensions { get; }

        IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
