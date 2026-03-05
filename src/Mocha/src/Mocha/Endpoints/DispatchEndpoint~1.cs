using System.Diagnostics;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Base class for typed dispatch endpoints that bind a specific configuration type to the dispatch lifecycle.
/// </summary>
/// <typeparam name="TConfiguration">The dispatch endpoint configuration type.</typeparam>
public abstract class DispatchEndpoint<TConfiguration>(MessagingTransport transport) : DispatchEndpoint(transport)
    where TConfiguration : DispatchEndpointConfiguration
{
    public new TConfiguration Configuration => (TConfiguration)base.Configuration;

    protected sealed override void OnInitialize(
        IMessagingConfigurationContext context,
        DispatchEndpointConfiguration configuration)
    {
        OnInitialize(context, (TConfiguration)configuration);
    }

    protected abstract void OnInitialize(IMessagingConfigurationContext context, TConfiguration configuration);

    protected sealed override void OnComplete(
        IMessagingConfigurationContext context,
        DispatchEndpointConfiguration configuration)
    {
        OnComplete(context, (TConfiguration)configuration);
    }

    protected abstract void OnComplete(IMessagingConfigurationContext context, TConfiguration configuration);
}
