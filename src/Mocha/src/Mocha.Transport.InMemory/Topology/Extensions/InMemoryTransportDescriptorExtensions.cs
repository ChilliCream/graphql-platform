namespace Mocha.Transport.InMemory;

/// <summary>
/// Extension methods for adding default conventions to an in-memory transport descriptor.
/// </summary>
public static class InMemoryTransportDescriptorExtensions
{
    internal static IInMemoryMessagingTransportDescriptor AddDefaults(
        this IInMemoryMessagingTransportDescriptor descriptor)
    {
        descriptor.AddConvention(new InMemoryDefaultReceiveEndpointEndpointConvention());
        descriptor.AddConvention(new InMemoryReceiveEndpointTopologyConvention());
        descriptor.AddConvention(new InMemoryDispatchEndpointTopologyConvention());

        return descriptor;
    }
}
