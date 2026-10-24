using Mocha.Transport.RabbitMQ.Middlewares;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for registering default conventions and middleware on a RabbitMQ transport descriptor.
/// </summary>
public static class RabbitMQTransportDescriptorExtensions
{
    internal static IRabbitMQMessagingTransportDescriptor AddDefaults(
        this IRabbitMQMessagingTransportDescriptor descriptor)
    {
        descriptor
            .UseReceive(RabbitMQReceiveMiddlewares.Acknowledgement, after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
        descriptor
            .UseReceive(RabbitMQReceiveMiddlewares.Parsing, after: RabbitMQReceiveMiddlewares.Acknowledgement.Key);

        ((IMessagingTransportDescriptor)descriptor).Extend().Configuration.DispatchPipelineModifiers
            .Prepend(RabbitMQDispatchMiddlewares.RoutingKey, before: null);

        return descriptor;
    }
}
