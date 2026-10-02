using Mocha.Middlewares;
using Mocha.Transport.Nats.Middlewares;

namespace Mocha.Transport.Nats;

/// <summary>
/// Extension methods for registering default conventions and middleware on a NATS transport
/// descriptor.
/// </summary>
public static class NatsTransportDescriptorExtensions
{
    internal static INatsMessagingTransportDescriptor AddDefaults(
        this INatsMessagingTransportDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor
            .Schema(NatsTransportConfiguration.DefaultSchema)
            .UseRoutingStrategy(static _ => new NatsRoutingStrategy());

        descriptor.UseReceive(
            NatsReceiveMiddlewares.Acknowledgement,
            after: ReceiveMiddlewares.ConcurrencyLimiter.Key);

        descriptor.UseReceive(
            NatsReceiveMiddlewares.Parsing,
            after: NatsReceiveMiddlewares.Acknowledgement.Key);

        return descriptor;
    }
}
