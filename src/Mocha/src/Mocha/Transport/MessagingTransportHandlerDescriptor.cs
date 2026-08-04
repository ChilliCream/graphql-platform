namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="IMessagingTransportHandlerDescriptor{TEndpointDescriptor}"/> that
/// passes endpoint configuration actions directly to the underlying endpoint descriptor.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class MessagingTransportHandlerDescriptor<TEndpointDescriptor>(TEndpointDescriptor endpoint)
    : IMessagingTransportHandlerDescriptor<TEndpointDescriptor>
{
    /// <inheritdoc />
    public IMessagingTransportHandlerDescriptor<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        configure(endpoint);
        return this;
    }
}
