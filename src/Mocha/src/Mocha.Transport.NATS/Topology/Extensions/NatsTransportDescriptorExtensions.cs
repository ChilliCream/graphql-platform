using Mocha.Middlewares;
using Mocha.Transport.NATS.Middlewares;

namespace Mocha.Transport.NATS;

/// <summary>
/// Extension methods for registering default conventions and middleware on a NATS transport descriptor.
/// </summary>
public static class NatsTransportDescriptorExtensions
{
    internal static INatsMessagingTransportDescriptor AddDefaults(
        this INatsMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new NatsDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new NatsReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new NatsDispatchEndpointTopologyConvention());

        descriptor.AppendReceive(ReceiveMiddlewares.ConcurrencyLimiter.Key, NatsReceiveMiddlewares.Acknowledgement);
        descriptor.AppendReceive(NatsReceiveMiddlewares.Acknowledgement.Key, NatsReceiveMiddlewares.Parsing);

        return descriptor;
    }
}
