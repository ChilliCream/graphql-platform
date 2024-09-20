namespace StrawberryShake;

/// <summary>
/// The result of a GraphQL operation.
/// </summary>
public interface IOperationResult<TResultData>: IOperationResult where TResultData : class
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

    /// <summary>
    /// Create a new result object with the specified data and dataInfo.
    /// </summary>
    /// <param name="data">
    /// The data of the new result object.
    /// </param>
    /// <param name="dataInfo">
    /// The data info describes the structure of the data object.
    /// </param>
    /// <returns>
    /// Returns the new result object with the specified data and dataInfo.
    /// </returns>
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
    /// Gets the errors that occurred during the execution.
    /// </summary>
    IReadOnlyList<IClientError> Errors { get; }

    /// <summary>
    /// Gets custom transport data specified by the server.
    /// </summary>
    IReadOnlyDictionary<string, object?> Extensions { get; }

    /// <summary>
    /// Gets custom context data provided by the transport pipeline.
    /// </summary>
    IReadOnlyDictionary<string, object?> ContextData { get; }
}
