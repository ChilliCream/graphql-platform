using Mocha.Transport.Kafka.Middlewares;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Extension methods for registering default conventions and middleware on a Kafka transport descriptor.
/// </summary>
public static class KafkaTransportDescriptorExtensions
{
    internal static IKafkaMessagingTransportDescriptor AddDefaults(
        this IKafkaMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new KafkaDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new KafkaReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new KafkaDispatchEndpointTopologyConvention());

        descriptor
            .UseReceive(KafkaReceiveMiddlewares.Commit, after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
        descriptor
            .UseReceive(KafkaReceiveMiddlewares.Parsing, after: KafkaReceiveMiddlewares.Commit.Key);

        return descriptor;
    }
}
