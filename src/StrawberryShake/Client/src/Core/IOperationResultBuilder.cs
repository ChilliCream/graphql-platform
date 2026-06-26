namespace StrawberryShake;

/// <summary>
/// The operation result builder will use the transport response and build from it
/// the operation result.
/// </summary>
/// <typeparam name="TResponseBody">
/// The serialized result.
/// </typeparam>
/// <typeparam name="TResultData">
/// The runtime result.
/// </typeparam>
public interface IOperationResultBuilder<TResponseBody, TResultData>
    where TResponseBody : class
    where TResultData : class
{
    /// <summary>
    /// Build runtime operation result.
    /// </summary>
    /// <param name="response">
    /// The serialized result.
    /// </param>
    /// <returns>
    /// Returns the runtime result.
    /// </returns>
    IOperationResult<TResultData> Build(
        Response<TResponseBody> response);

    /// <summary>
    /// Builds a runtime operation result from a previously persisted transport "data"
    /// payload, without executing the operation. This is used to rehydrate state that was
    /// captured during a server prerender.
    /// </summary>
    /// <param name="persistedData">
    /// The UTF-8 encoded JSON of the GraphQL response "data" object.
    /// </param>
    /// <returns>
    /// Returns the runtime result.
    /// </returns>
    IOperationResult<TResultData> BuildFromPersistedData(ReadOnlyMemory<byte> persistedData)
        => throw new NotSupportedException(
            "This operation result builder does not support rehydrating from persisted data.");
}
