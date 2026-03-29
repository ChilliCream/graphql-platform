namespace Mocha;

/// <summary>
/// Fluent configurator returned by a transport's <c>Handler&lt;T&gt;()</c> method.
/// Allows configuring the receive endpoint for a claimed handler.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
public interface ITransportHandlerConfigurator<TEndpointDescriptor>
{
    /// <summary>
    /// Configures the receive endpoint for this handler.
    /// Can be called multiple times - actions compose in order.
    /// </summary>
    /// <param name="configure">A delegate to configure the endpoint descriptor.</param>
    /// <returns>This configurator for method chaining.</returns>
    ITransportHandlerConfigurator<TEndpointDescriptor> ConfigureEndpoint(Action<TEndpointDescriptor> configure);
}
