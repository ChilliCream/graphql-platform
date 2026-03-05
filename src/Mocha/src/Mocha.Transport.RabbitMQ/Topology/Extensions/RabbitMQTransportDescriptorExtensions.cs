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
        descriptor.AddConvention(new RabbitMQDefaultReceiveEndpointEndpointConvention());
        descriptor.AddConvention(new RabbitMQReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new RabbitMQDispatchEndpointTopologyConvention());

        descriptor.AppendReceive(ReceiveMiddlewares.ConcurrencyLimiter.Key, RabbitMQReceiveMiddlewares.Acknowledgement);
        descriptor.AppendReceive(RabbitMQReceiveMiddlewares.Acknowledgement.Key, RabbitMQReceiveMiddlewares.Parsing);

        return descriptor;
    }
}
