using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// Creates the transport's connection owners without a transport around them.
/// </summary>
internal static class TestManagers
{
    public static RabbitMQConsumerManager CreateConsumerManager(
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        string connectionName = "test consumer",
        ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<RabbitMQConsumerManager>();
        return new RabbitMQConsumerManager(
            logger,
            new RabbitMQConnectionManager(
                logger,
                connectionName,
                TimeProvider.System,
                connectionFactory,
                RabbitMQConsumerManager.SetUpConnectionAsync));
    }

    public static RabbitMQDispatcher CreateDispatcher(
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        Func<IConnection, CancellationToken, Task> setUpConnectionAsync,
        string connectionName = "test dispatcher",
        ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger<RabbitMQDispatcher>();
        return new RabbitMQDispatcher(
            logger,
            new RabbitMQConnectionManager(
                logger,
                connectionName,
                TimeProvider.System,
                connectionFactory,
                setUpConnectionAsync));
    }

    /// <summary>
    /// Registers a consumer that consumes at once, without preparing its channel.
    /// </summary>
    public static Task<RabbitMQConsumer> RegisterConsumerAsync(
        this RabbitMQConsumerManager manager,
        string queueName,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort maxPrefetch,
        ushort maxConcurrency,
        CancellationToken cancellationToken)
        => manager.RegisterConsumerAsync(
            queueName,
            prepareAsync: null,
            messageHandler,
            maxPrefetch,
            maxConcurrency,
            cancellationToken);
}
