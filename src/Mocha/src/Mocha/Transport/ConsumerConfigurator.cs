namespace Mocha;

/// <summary>
/// Internal implementation of <see cref="IConsumerConfigurator{TEndpointDescriptor}"/> that
/// captures endpoint configuration actions on the underlying <see cref="HandlerClaim"/>.
/// </summary>
/// <typeparam name="TEndpointDescriptor">The endpoint descriptor type exposed by the transport.</typeparam>
internal sealed class ConsumerConfigurator<TEndpointDescriptor>
    : IConsumerConfigurator<TEndpointDescriptor>
{
    private readonly HandlerClaim _claim;

    internal ConsumerConfigurator(HandlerClaim claim) => _claim = claim;

    /// <inheritdoc />
    public IConsumerConfigurator<TEndpointDescriptor> ConfigureEndpoint(
        Action<TEndpointDescriptor> configure)
    {
        var prev = _claim.ConfigureEndpoint;
        _claim.ConfigureEndpoint = prev is null
            ? obj => configure((TEndpointDescriptor)obj)
            : obj => { prev(obj); configure((TEndpointDescriptor)obj); };
        return this;
    }
}
