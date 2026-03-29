namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="IHandlerConfigurator{TEndpointDescriptor}"/> that
/// captures endpoint configuration actions on the underlying <see cref="HandlerClaim"/>.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class HandlerConfigurator<TEndpointDescriptor>
    : IHandlerConfigurator<TEndpointDescriptor>
{
    private readonly HandlerClaim _claim;

    internal HandlerConfigurator(HandlerClaim claim) => _claim = claim;

    /// <inheritdoc />
    public IHandlerConfigurator<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        var prev = _claim.ConfigureEndpoint;
        _claim.ConfigureEndpoint = prev is null
            ? obj => configure((TEndpointDescriptor)obj)
            : obj => { prev(obj); configure((TEndpointDescriptor)obj); };
        return this;
    }
}
