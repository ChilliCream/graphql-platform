using System.Diagnostics.CodeAnalysis;

namespace StrawberryShake;

/// <summary>
/// The operation store tracks and stores results by requests.
/// </summary>
public interface IOperationStore : IDisposable
{
    /// <summary>
    /// Stores the <paramref name="operationResult"/> for the specified
    /// <paramref name="operationRequest"/>.
    /// </summary>
    /// <param name="operationRequest">
    /// The operation request for which a result shall be stored.
    /// </param>
    /// <param name="operationResult">
    /// The operation result that shall be stored.
    /// </param>
    /// <typeparam name="TResultData">
    /// The type of result data.
    /// </typeparam>
    void Set<TResultData>(
        OperationRequest operationRequest,
        IOperationResult<TResultData> operationResult)
        where TResultData : class;

    /// <summary>
    /// Stores the <paramref name="operationResult"/> for the specified
    /// <paramref name="operationRequest"/>.
    /// </summary>
    /// <param name="operationRequest">
    /// The operation request for which a result shall be stored.
    /// </param>
    /// <param name="operationResult">
    /// The operation result that shall be stored.
    /// </param>
    void Set(
        OperationRequest operationRequest,
        IOperationResult operationResult);

    /// <summary>
    /// Resets the stored operation by removing the cached result.
    ///
    /// This marks an operation as dirty meaning that whenever a new subscriber comes
    /// the result will be re-fetched from the network.
    /// </summary>
    /// <param name="operationRequest">
    /// The operation request for which a result shall be stored.
    /// </param>
    void Reset(OperationRequest operationRequest);

    /// <summary>
    /// Removes the operation and completes all subscriptions.
    /// </summary>
    /// <param name="operationRequest">
    /// The request that shall be completed.
    /// </param>
    void Remove(OperationRequest operationRequest);

    /// <summary>
    /// Removes all operations and completes all subscriptions.
    /// </summary>
    void Clear();

    /// <summary>
    /// Tries to retrieve for a <paramref name="operationRequest"/>.
    /// </summary>
    /// <param name="operationRequest">
    /// The operation request for which a result shall be retrieved.
    /// </param>
    /// <param name="result">
    /// The retrieved operation result.
    /// </param>
    /// <typeparam name="TResultData">
    /// The type of result data.
    /// </typeparam>
    /// <returns>
    /// <c>true</c>, a result was found for the specified <paramref name="operationRequest"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool TryGet<TResultData>(
        OperationRequest operationRequest,
        [NotNullWhen(true)] out IOperationResult<TResultData>? result)
        where TResultData : class;

    /// <summary>
    /// Gets a snapshot of the current stored operations.
    /// </summary>
    IEnumerable<StoredOperationVersion> GetAll();

    /// <summary>
    /// Gets a list of entities that are linked to operation results.
    /// </summary>
    IReadOnlyList<EntityId> GetUsedEntityIds();

    /// <summary>
    /// Watches for updates to a <paramref name="operationRequest"/>.
    /// </summary>
    /// <param name="operationRequest">
    /// The operation request that is being observed.
    /// </param>
    /// <typeparam name="TResultData">
    /// The type of result data.
    /// </typeparam>
    /// <returns>
    /// Returns an operation observable which can be used to observe
    /// updates to the result of the specified <paramref name="operationRequest"/>.
    /// </returns>
    IObservable<IOperationResult<TResultData>> Watch<TResultData>(
        OperationRequest operationRequest)
        where TResultData : class;

    /// <summary>
    /// Watches all updates to this store.
    /// </summary>
    /// <returns>
    /// Returns an operation update observable which can be used to observe
    /// updates to the <see cref="IOperationStore"/>.
    /// </returns>
    IObservable<OperationUpdate> Watch();
}
