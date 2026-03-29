namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="ITransportConsumerConfigurator{TEndpointDescriptor}"/> that
/// passes endpoint configuration actions directly to the underlying endpoint descriptor.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class TransportConsumerConfigurator<TEndpointDescriptor>(TEndpointDescriptor endpoint)
    : ITransportConsumerConfigurator<TEndpointDescriptor>
{
    /// <inheritdoc />
    public ITransportConsumerConfigurator<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        configure(endpoint);
        return this;
    }
}
