using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    /// <summary>
    /// The result of a GraphQL operation.
    /// </summary>
    public interface IOperationResult<T> : IOperationResult where T : class
    {
        /// <summary>
        /// Gets the data object or <c>null</c>.
        /// </summary>
        new T? Data { get; }

        /// <summary>
        /// Gets the data factory which can build from the
        /// <see cref="DataInfo"/> a new <see cref="Data"/>.
        /// </summary>
        new IOperationResultDataFactory<T> DataFactory { get; }

        IOperationResult<T> WithData(T data, IOperationResultDataInfo dataInfo);
    }

    /// <summary>
    /// The result of a GraphQL operation.
    /// </summary>
    public interface IOperationResult
    {
        /// <summary>
        /// Gets the data object or <c>null</c>.
        /// </summary>
        object? Data { get; }

        /// <summary>
        /// Gets the type of the data object.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Gets the data info which contains information on how to
        /// construct data from the entity store.
        /// </summary>
        IOperationResultDataInfo? DataInfo { get; }

        /// <summary>
        /// Gets the data factory which can build from the
        /// <see cref="DataInfo"/> a new <see cref="Data"/>.
        /// </summary>
        object DataFactory { get; }

        /// <summary>
        /// Gets the errors that occured during the execution.
        /// </summary>
        IReadOnlyList<IClientError> Errors { get; }

        IReadOnlyDictionary<string, object?> Extensions { get; }

        IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
