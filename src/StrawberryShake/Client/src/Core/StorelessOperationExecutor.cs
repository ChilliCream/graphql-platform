using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Properties;

namespace StrawberryShake
{
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

        public StorelessOperationExecutor(
            IConnection<TData> connection,
            Func<IOperationResultBuilder<TData, TResult>> resultBuilder)
        {
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _resultBuilder = resultBuilder ??
                throw new ArgumentNullException(nameof(resultBuilder));
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
            }

            if (result is null)
            {
                throw new InvalidOperationException(Resources.HttpOperationExecutor_ExecuteAsync_NoResult);
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

            return new StorelessOperationExecutorObservable(_connection, _resultBuilder, request);
        }
    }
}
