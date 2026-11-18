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
        => CreateConsumerManager(connectionFactory, new RabbitMQConsumerManagerOptions());

    public static RabbitMQConsumerManager CreateConsumerManager(
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        RabbitMQConsumerManagerOptions options)
        => new(
            new RabbitMQConsumerLogger(NullLogger.Instance),
            TimeProvider.System,
            options,
            new RabbitMQConnectionManager(
                new RabbitMQConnectionLogger(NullLogger.Instance, RabbitMQConnectionRole.Consumer),
                TimeProvider.System,
                new RabbitMQConnectionManagerOptions(),
                connectionFactory,
                RabbitMQConsumerManager.SetUpConnectionAsync));

    /// <summary>
    /// Gets whether the connection is ready and every consumer is attached.
    /// </summary>
    public static bool AreConsumersAttached(this RabbitMQConsumerManager manager)
        => manager.IsConnected && manager.Consumers.All(consumer => consumer.IsAttached);

    /// <summary>
    /// Gets whether the consumer of a queue is attached.
    /// </summary>
    public static bool IsConsumerAttached(this RabbitMQConsumerManager manager, string queueName)
        => manager.Consumers.Any(consumer => consumer.QueueName == queueName && consumer.IsAttached);

    /// <summary>
    /// Gets whether a consumer waits for a handler of a lost channel to return.
    /// </summary>
    public static bool HasBlockedConsumer(this RabbitMQConsumerManager manager)
        => manager.Consumers.Any(consumer => consumer.State is ConsumerState.WaitingForHandlers);

    /// <summary>
    /// Runs the consumer health check of a transport and returns the reported reasons.
    /// </summary>
    public static async Task<string[]> CheckConsumerHealthAsync(this RabbitMQMessagingTransport transport)
    {
        var context = new MessageBusHealthContext();
        await new RabbitMQConsumerHealthCheck(transport).CheckAsync(context, CancellationToken.None);
        return [.. context.Failures.Select(failure => failure.Reason)];
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
