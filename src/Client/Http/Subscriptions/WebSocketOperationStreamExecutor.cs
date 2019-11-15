using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    public class WebSocketOperationStreamExecutor
        : IOperationStreamExecutor
    {
        private readonly ISocketConnectionPool _connectionPool;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly string _socketName;

        public WebSocketOperationStreamExecutor(
            ISocketConnectionPool connectionPool,
            ISubscriptionManager subscriptionManager,
            string socketName)
        {
            _connectionPool = connectionPool
                ?? throw new ArgumentNullException(nameof(connectionPool));
            _subscriptionManager = subscriptionManager
                ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _socketName = socketName
                ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public Task<IResponseStream> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {

        }

        public Task<IResponseStream<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class
        {
            throw new System.NotImplementedException();
        }
    }
}
