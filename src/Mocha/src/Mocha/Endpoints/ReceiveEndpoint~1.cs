namespace Mocha;

/// <summary>
/// Base class for typed receive endpoints that bind a specific configuration type to the receive lifecycle.
/// </summary>
/// <typeparam name="TConfiguration">The receive endpoint configuration type.</typeparam>
public abstract class ReceiveEndpoint<TConfiguration>(MessagingTransport transport) : ReceiveEndpoint(transport)
    where TConfiguration : ReceiveEndpointConfiguration
{
    public new TConfiguration Configuration => (TConfiguration)base.Configuration;

    protected sealed override void OnInitialize(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        OnInitialize(context, (TConfiguration)configuration);
    }

    protected abstract void OnInitialize(IMessagingConfigurationContext context, TConfiguration configuration);

    protected sealed override void OnComplete(
        IMessagingConfigurationContext context,
        ReceiveEndpointConfiguration configuration)
    {
        OnComplete(context, (TConfiguration)configuration);
    }

    protected abstract void OnComplete(IMessagingConfigurationContext context, TConfiguration configuration);
}
