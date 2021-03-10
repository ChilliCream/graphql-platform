using System;
using System.Threading;
using System.Threading.Tasks;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake
{
    /// <summary>
    /// The operation executor handles the execution of a specific operation.
    /// </summary>
    /// <typeparam name="TResultData">
    /// The result data type of this operation executor.
    /// </typeparam>
    public partial class OperationExecutor<TData, TResult>
        : IOperationExecutor<TResult>
        where TResult : class
        where TData : class
    {
        private readonly IConnection<TData> _connection;
        private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
        private readonly IOperationStore _operationStore;
        private readonly ExecutionStrategy _strategy;

        public OperationExecutor(
            IConnection<TData> connection,
            Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
            IOperationStore operationStore,
            ExecutionStrategy strategy = ExecutionStrategy.NetworkOnly)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _resultBuilder = resultBuilder ??
                throw new ArgumentNullException(nameof(resultBuilder));
            _operationStore = operationStore ??
                throw new ArgumentNullException(nameof(operationStore));
            _strategy = strategy;
        }

        /// <inheritdocs />
        public async Task<IOperationResult<TResult>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IOperationResult<TResult>? result = null;
            IOperationResultBuilder<TData, TResult> resultBuilder = _resultBuilder();

            await foreach (var response in _connection.ExecuteAsync(request, cancellationToken))
            {
                result = resultBuilder.Build(response);
                _operationStore.Set(request, result);
            }

            if (result is null)
            {
                throw new InvalidOperationException(HttpOperationExecutor_ExecuteAsync_NoResult);
            }

            return result;
        }

        /// <inheritdocs />
        public IObservable<IOperationResult<TResult>> Watch(
            OperationRequest request,
            ExecutionStrategy? strategy = null)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new OperationExecutorObservable(
                _connection,
                _operationStore,
                _resultBuilder,
                request,
                strategy ?? _strategy);
        }
    }
}
