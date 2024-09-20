using StrawberryShake.Properties;

namespace StrawberryShake;

/// <summary>
/// The no store operation executor handles the execution of a specific operation without
/// involving store updates
/// </summary>
/// <typeparam name="TData">
/// The result data type of this operation executor.
/// </typeparam>
/// <typeparam name="TResult">
/// The runtime result
/// </typeparam>
public partial class StorelessOperationExecutor<TData, TResult>
    : IOperationExecutor<TResult>
    where TData : class
    where TResult : class
{
    private readonly IConnection<TData> _connection;
    private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
    private readonly Func<IResultPatcher<TData>> _resultPatcher;

    public StorelessOperationExecutor(
        IConnection<TData> connection,
        Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
        Func<IResultPatcher<TData>> resultPatcher)
    {
        _connection = connection ??
            throw new ArgumentNullException(nameof(connection));
        _resultBuilder = resultBuilder ??
            throw new ArgumentNullException(nameof(resultBuilder));
        _resultPatcher = resultPatcher ??
            throw new ArgumentNullException(nameof(resultPatcher));
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
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

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
                result = resultBuilder.Build(response);
            }
            else
            {
                resultPatcher.SetResponse(response);
                result = resultBuilder.Build(response);
            }
        }

        if (result is null)
        {
            throw new InvalidOperationException(Resources.HttpOperationExecutor_ExecuteAsync_NoResult);
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
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new StorelessOperationExecutorObservable(_connection, _resultBuilder, request);
    }
}
