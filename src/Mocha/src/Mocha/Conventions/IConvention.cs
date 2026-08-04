namespace Mocha;

/// <summary>
/// Marker interface for all messaging conventions that customize bus behavior during setup.
/// </summary>
public interface IConvention;

/// <summary>
/// A typed configuration convention that applies only to configurations of type
/// <typeparamref name="TConfiguration"/>.
/// </summary>
/// <typeparam name="TConfiguration">The specific configuration type this convention applies to.</typeparam>
public interface IConfigurationConvention<in TConfiguration> : IConfigurationConvention
{
    void IConfigurationConvention.Configure(
        IMessagingConfigurationContext context,
        MessagingConfiguration configuration)
    {
        if (configuration is not TConfiguration configurationOfT)
        {
            return;
        }

        Configure(context, configurationOfT);
    }

    /// <summary>
    /// Applies convention-based configuration to a configuration object of the specified type.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The typed configuration object to modify.</param>
    void Configure(IMessagingConfigurationContext context, TConfiguration configuration);
}
