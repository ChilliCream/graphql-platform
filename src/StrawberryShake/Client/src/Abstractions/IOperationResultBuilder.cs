namespace StrawberryShake
{
    /// <summary>
    /// The operation result builder will use the transport response and build from it
    /// the operation result.
    /// </summary>
    /// <typeparam name="TData">
    /// The serialized result.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The runtime result.
    /// </typeparam>
    public interface IOperationResultBuilder<TData, out TResult>
        where TResult : class
        where TData : class
    {
        /// <summary>
        /// Build runtime operation result.
        /// </summary>
        /// <param name="data">
        /// The serialized result.
        /// </param>
        /// <returns>
        /// Returns the runtime result.
        /// </returns>
        IOperationResult<TResult> Build(Response<TData> data);
    }
}
