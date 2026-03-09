namespace Mocha;

/// <summary>
/// A convention that applies cross-cutting configuration to any
/// <see cref="MessagingConfiguration"/> during bus setup.
/// </summary>
public interface IConfigurationConvention : IConvention
{
    /// <summary>
    /// Applies convention-based configuration to the given configuration object.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="configuration">The configuration object to modify.</param>
    void Configure(IMessagingConfigurationContext context, MessagingConfiguration configuration);
}
