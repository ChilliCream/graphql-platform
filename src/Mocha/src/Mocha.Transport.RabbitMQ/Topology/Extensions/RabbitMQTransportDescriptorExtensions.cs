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
        descriptor.AddConvention(new RabbitMQDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new RabbitMQReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new RabbitMQDispatchEndpointTopologyConvention());

        descriptor
            .UseReceive(RabbitMQReceiveMiddlewares.Acknowledgement, after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
        descriptor
            .UseReceive(RabbitMQReceiveMiddlewares.Parsing, after: RabbitMQReceiveMiddlewares.Acknowledgement.Key);

        descriptor
            .UseDispatch(RabbitMQDispatchMiddlewares.RoutingKey, before: DispatchMiddlewares.Serialization.Key);

        return descriptor;
    }
}
