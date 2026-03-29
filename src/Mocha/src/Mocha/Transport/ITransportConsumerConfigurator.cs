namespace Mocha;

/// <summary>
/// Fluent configurator returned by a transport's <c>Consumer&lt;T&gt;()</c> method.
/// Allows configuring the receive endpoint for a claimed consumer.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
public interface ITransportConsumerConfigurator<TEndpointDescriptor>
{
    /// <summary>
    /// Configures the receive endpoint for this consumer.
    /// Can be called multiple times - actions compose in order.
    /// </summary>
    /// <param name="configure">A delegate to configure the endpoint descriptor.</param>
    /// <returns>This configurator for method chaining.</returns>
    ITransportConsumerConfigurator<TEndpointDescriptor> ConfigureEndpoint(Action<TEndpointDescriptor> configure);
}
