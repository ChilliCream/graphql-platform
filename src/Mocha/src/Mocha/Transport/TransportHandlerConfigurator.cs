namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="ITransportHandlerConfigurator{TEndpointDescriptor}"/> that
/// passes endpoint configuration actions directly to the underlying endpoint descriptor.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class TransportHandlerConfigurator<TEndpointDescriptor>(TEndpointDescriptor endpoint)
    : ITransportHandlerConfigurator<TEndpointDescriptor>
{
    /// <inheritdoc />
    public ITransportHandlerConfigurator<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        configure(endpoint);
        return this;
    }
}
