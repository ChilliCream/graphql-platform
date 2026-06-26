using static StrawberryShake.Properties.Resources;

namespace StrawberryShake;

/// <summary>
/// The operation executor handles the execution of a specific operation.
/// </summary>
/// <typeparam name="TData">
/// The result data type of this operation executor.
/// </typeparam>
/// <typeparam name="TResult">
/// The runtime result
/// </typeparam>
public partial class OperationExecutor<TData, TResult>
    : IOperationExecutor<TResult>
    where TData : class
    where TResult : class

{
    private readonly IConnection<TData> _connection;
    private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
    private readonly Func<IResultPatcher<TData>> _resultPatcher;
    private readonly IOperationStore _operationStore;
    private readonly ExecutionStrategy _strategy;

    public OperationExecutor(
        IConnection<TData> connection,
        Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
        Func<IResultPatcher<TData>> resultPatcher,
        IOperationStore operationStore,
        ExecutionStrategy strategy = ExecutionStrategy.NetworkOnly)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(resultBuilder);
        ArgumentNullException.ThrowIfNull(resultPatcher);
        ArgumentNullException.ThrowIfNull(operationStore);

        _connection = connection;
        _resultBuilder = resultBuilder;
        _resultPatcher = resultPatcher;
        _operationStore = operationStore;
        _strategy = strategy;
    }

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
    public async Task<IOperationResult<TResult>> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IOperationResult<TResult>? result = null;
        var resultBuilder = _resultBuilder();
        var resultPatcher = _resultPatcher();

        await foreach (var response in
            _connection.ExecuteAsync(request)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
        {
            if (response.IsPatch)
            {
                var patched = resultPatcher.PatchResponse(response);
                result = resultBuilder.Build(patched);
                _operationStore.Set(request, result);
            }
            else
            {
                resultPatcher.SetResponse(response);
                result = resultBuilder.Build(response);
                _operationStore.Set(request, result);
            }
        }

        if (result is null)
        {
            throw new InvalidOperationException(HttpOperationExecutor_ExecuteAsync_NoResult);
        }

        return result;
    }

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
    public IObservable<IOperationResult<TResult>> Watch(
        OperationRequest request,
        ExecutionStrategy? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new OperationExecutorObservable(
            _connection,
            _operationStore,
            _resultBuilder,
            _resultPatcher,
            request,
            strategy ?? _strategy);
    }

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
    public IObservable<IOperationResult<TResult>> Watch(
        OperationRequest request,
        ReadOnlyMemory<byte>? persistedState,
        ExecutionStrategy? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (persistedState is { } state)
        {
            // Rehydrate the persisted payload through the existing deserialization path so
            // the entity store and operation store are seeded, then serve it from the cache.
            var result = _resultBuilder().BuildFromPersistedData(state);
            _operationStore.Set(request, result);
            strategy ??= ExecutionStrategy.CacheFirst;
        }

        return new OperationExecutorObservable(
            _connection,
            _operationStore,
            _resultBuilder,
            _resultPatcher,
            request,
            strategy ?? ExecutionStrategy.CacheFirst);
    }
}
