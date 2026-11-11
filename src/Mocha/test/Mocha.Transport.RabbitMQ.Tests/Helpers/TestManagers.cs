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
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory)
    {
        var options = new RabbitMQConnectionManagerOptions();
        return new RabbitMQConsumerManager(
            NullLogger.Instance,
            TimeProvider.System,
            options.CloseTimeout,
            new RabbitMQConnectionManager(
                new RabbitMQConnectionLogger(NullLogger.Instance, RabbitMQConnectionRole.Consumer),
                TimeProvider.System,
                options,
                connectionFactory,
                RabbitMQConsumerManager.SetUpConnectionAsync));
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
