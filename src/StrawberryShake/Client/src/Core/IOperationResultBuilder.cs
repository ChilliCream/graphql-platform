namespace StrawberryShake
{
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
        /// Patch runtime operation result.
        /// </summary>
        /// <param name="response">
        /// The serialized result.
        /// </param>
        /// <param name="result">
        /// The previous result.
        /// </param>
        /// <returns>
        /// Returns the runtime result.
        /// </returns>
        IOperationResult<TResultData> Patch(
            Response<TResponseBody> response,
            IOperationResult<TResultData> result);
    }
}
