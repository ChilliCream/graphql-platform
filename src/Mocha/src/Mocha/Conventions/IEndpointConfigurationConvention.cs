namespace Mocha;

/// <summary>
/// A convention that applies transport-aware configuration to any
/// <see cref="MessagingConfiguration"/> during endpoint setup.
/// </summary>
/// <remarks>
/// Unlike <see cref="IConfigurationConvention"/>, this convention receives the owning
/// <see cref="MessagingTransport"/> instance, allowing transport-specific conventions to
/// access topology defaults and schema without searching through the transport collection.
/// </remarks>
public interface IEndpointConfigurationConvention : IConvention
{
    /// <summary>
    /// Applies convention-based configuration to the given configuration object with access to the owning transport.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The messaging transport that owns this endpoint.</param>
    /// <param name="configuration">The configuration object to modify.</param>
    void Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        MessagingConfiguration configuration);
}

/// <summary>
/// A typed endpoint configuration convention that applies only to configurations of type
/// <typeparamref name="TConfiguration"/> and receives the owning transport instance.
/// </summary>
/// <typeparam name="TConfiguration">The specific configuration type this convention applies to.</typeparam>
public interface IEndpointConfigurationConvention<in TConfiguration> : IEndpointConfigurationConvention
    where TConfiguration : MessagingConfiguration
{
    void IEndpointConfigurationConvention.Configure(
        IMessagingConfigurationContext context,
        MessagingTransport transport,
        MessagingConfiguration configuration)
    {
        if (configuration is not TConfiguration configurationOfT)
        {
            return;
        }

        Configure(context, transport, configurationOfT);
    }

    /// <summary>
    /// Applies convention-based configuration to a configuration object of the specified type
    /// with access to the owning transport.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transport">The messaging transport that owns this endpoint.</param>
    /// <param name="configuration">The typed configuration object to modify.</param>
    void Configure(IMessagingConfigurationContext context, MessagingTransport transport, TConfiguration configuration);
}
