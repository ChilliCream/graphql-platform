using Mocha.Transport.AzureServiceBus.Middlewares;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods for registering default conventions and middleware on an Azure Service Bus transport descriptor.
/// </summary>
public static class AzureServiceBusTransportDescriptorExtensions
{
    internal static IAzureServiceBusMessagingTransportDescriptor AddDefaults(
        this IAzureServiceBusMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new AzureServiceBusDefaultReceiveEndpointConvention());
        descriptor.AddConvention(new AzureServiceBusReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new AzureServiceBusDispatchEndpointTopologyConvention());

        descriptor.UseReceive(
            AzureServiceBusReceiveMiddlewares.Acknowledgement,
            after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
        descriptor.UseReceive(
            AzureServiceBusReceiveMiddlewares.Parsing,
            after: AzureServiceBusReceiveMiddlewares.Acknowledgement.Key);

        descriptor.UseDispatch(
            AzureServiceBusDispatchMiddlewares.MessageProperties,
            before: DispatchMiddlewares.Serialization.Key);

        return descriptor;
    }
}
