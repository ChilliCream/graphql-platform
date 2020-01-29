using System.Threading;
using System;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
{
    public class SocketOperationStreamExecutorFactory
        : IOperationStreamExecutorFactory
    {
        private readonly Func<CancellationToken, Task<ISocketConnection>> _connectionFactory;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IOperationFormatter _operationFormatter;
        private readonly IResultParserCollection _resultParserResolver;

        public SocketOperationStreamExecutorFactory(
            string name,
            Func<string, CancellationToken, Task<ISocketConnection>> connectionFactory,
            ISubscriptionManager subscriptionManager,
            IOperationFormatter operationFormatter,
            IResultParserCollection resultParserResolver)
        {
            if (connectionFactory is null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            _connectionFactory = ct => connectionFactory(name, ct);
            _subscriptionManager = subscriptionManager
                ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _operationFormatter = operationFormatter
                ?? throw new ArgumentNullException(nameof(operationFormatter));
            _resultParserResolver = resultParserResolver
                ?? throw new ArgumentNullException(nameof(resultParserResolver));
        }

        public string Name { get; }

        public IOperationStreamExecutor CreateStreamExecutor()
        {
            return new SocketOperationStreamExecutor(
                _connectionFactory,
                _subscriptionManager,
                _operationFormatter,
                _resultParserResolver);
        }
    }
}
