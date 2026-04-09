using Mocha.Transport.AzureEventHub.Middlewares;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Extension methods for registering default conventions and middleware on an Event Hub transport descriptor.
/// </summary>
public static class EventHubTransportDescriptorExtensions
{
    internal static IEventHubMessagingTransportDescriptor AddDefaults(
        this IEventHubMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new EventHubDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new EventHubReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new EventHubDispatchEndpointTopologyConvention());

        descriptor.UseReceive(
            EventHubReceiveMiddlewares.Acknowledgement,
            after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
        descriptor.UseReceive(
            EventHubReceiveMiddlewares.Parsing,
            after: EventHubReceiveMiddlewares.Acknowledgement.Key);

        return descriptor;
    }
}
