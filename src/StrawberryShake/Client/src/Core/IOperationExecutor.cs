namespace StrawberryShake;

/// <summary>
/// The operation executor handles the execution of a specific operation.
/// </summary>
/// <typeparam name="TResultData">
/// The result data type of this operation executor.
/// </typeparam>
public interface IOperationExecutor<TResultData> where TResultData : class
{
    /// <summary>
    /// Executes the result and returns the result.
    /// </summary>
    /// <param name="request">
    /// The operation request.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns the operation result.
    /// </returns>
    Task<IOperationResult<TResultData>> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a requests and subscribes to updates on the request results.
    /// </summary>
    /// <param name="request">
    /// The operation request.
    /// </param>
    /// <param name="strategy">
    /// The request execution strategy.
    /// </param>
    /// <returns>
    /// The observable that can be used to subscribe to results.
    /// </returns>
    IObservable<IOperationResult<TResultData>> Watch(
        OperationRequest request,
        ExecutionStrategy? strategy = null);
}
