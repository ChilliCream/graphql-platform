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

    /// <summary>
    /// Registers a request and subscribes to updates on the request results, optionally
    /// seeding the store from a previously persisted transport payload so that the operation
    /// is not re-executed. This is used to rehydrate prerendered Blazor components.
    /// </summary>
    /// <param name="request">
    /// The operation request.
    /// </param>
    /// <param name="persistedState">
    /// The UTF-8 encoded JSON of the GraphQL response "data" object captured during a server
    /// prerender, or <c>null</c> to execute the operation normally.
    /// </param>
    /// <param name="strategy">
    /// The request execution strategy.
    /// </param>
    /// <returns>
    /// The observable that can be used to subscribe to results.
    /// </returns>
    IObservable<IOperationResult<TResultData>> Watch(
        OperationRequest request,
        ReadOnlyMemory<byte>? persistedState,
        ExecutionStrategy? strategy = null);
}
