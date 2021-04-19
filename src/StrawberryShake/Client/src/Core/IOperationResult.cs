using System;
using System.Collections.Generic;

namespace StrawberryShake
{
    /// <summary>
    /// The result of a GraphQL operation.
    /// </summary>
    public interface IOperationResult<TResultData>
        : IOperationResult
        where TResultData : class
    {
        /// <summary>
        /// Gets the data object or <c>null</c>.
        /// </summary>
        new TResultData? Data { get; }

        /// <summary>
        /// Gets the data factory which can build from the
        /// <see cref="IOperationResultDataInfo"/> a new <see cref="Data"/>.
        /// </summary>
        new IOperationResultDataFactory<TResultData> DataFactory { get; }

        IOperationResult<TResultData> WithData(TResultData data, IOperationResultDataInfo dataInfo);
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
