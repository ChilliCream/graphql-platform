namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="IMessagingTransportConsumerDescriptor{TEndpointDescriptor}"/> that
/// passes endpoint configuration actions directly to the underlying endpoint descriptor.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class MessagingTransportConsumerDescriptor<TEndpointDescriptor>(TEndpointDescriptor endpoint)
    : IMessagingTransportConsumerDescriptor<TEndpointDescriptor>
{
    /// <inheritdoc />
    public IMessagingTransportConsumerDescriptor<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        configure(endpoint);
        return this;
    }
}
