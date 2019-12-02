using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    public class SocketOperationStreamExecutor
        : IOperationStreamExecutor
    {
        Func<CancellationToken, Task<ISocketConnection>> _connectionFactory;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IResultParserResolver _resultParserResolver;

        public SocketOperationStreamExecutor(
            Func<CancellationToken, Task<ISocketConnection>> connectionFactory,
            ISubscriptionManager subscriptionManager,
            IResultParserResolver resultParserResolver)
        {
            _connectionFactory = connectionFactory
                ?? throw new ArgumentNullException(nameof(connectionFactory));
            _subscriptionManager = subscriptionManager
                ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _resultParserResolver = resultParserResolver
                ?? throw new ArgumentNullException(nameof(resultParserResolver));
        }

        public Task<IResponseStream> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.Kind != OperationKind.Subscription)
            {
                throw new ArgumentNullException(
                    "This execution can only execute subscriptions.",
                    nameof(operation));
            }

            return ExecuteInternalAsync(operation, cancellationToken);
        }

        public async Task<IResponseStream> ExecuteInternalAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            IResultParser resultParser =
                _resultParserResolver.GetResultParser(operation.ResultType);
            var subscription = Subscription.New(operation, resultParser);

            ISocketConnection connection = await _connectionFactory(cancellationToken)
                .ConfigureAwait(false);
            await _subscriptionManager.RegisterAsync(subscription, connection)
                .ConfigureAwait(false);

            return subscription;
        }

        public Task<IResponseStream<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.Kind != OperationKind.Subscription)
            {
                throw new ArgumentNullException(
                    "This execution can only execute subscriptions.",
                    nameof(operation));
            }

            return ExecuteInternalAsync(operation, cancellationToken);
        }

        public async Task<IResponseStream<T>> ExecuteInternalAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class
        {
            IResultParser resultParser =
                _resultParserResolver.GetResultParser(operation.ResultType);
            var subscription = Subscription.New(operation, resultParser);

            ISocketConnection connection = await _connectionFactory(cancellationToken)
                .ConfigureAwait(false);
            await _subscriptionManager.RegisterAsync(subscription, connection)
                .ConfigureAwait(false);

            return subscription;
        }
    }
}
