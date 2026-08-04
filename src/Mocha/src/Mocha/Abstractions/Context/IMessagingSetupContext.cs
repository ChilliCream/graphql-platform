namespace Mocha;

/// <summary>
/// Represents the context available during the setup phase of a transport, providing access to the
/// full messaging configuration and error reporting.
/// </summary>
public interface IMessagingSetupContext : IMessagingConfigurationContext, IFeatureProvider
{
    /// <summary>
    /// Gets the transport currently being set up, or <c>null</c> if setup is running at the bus
    /// level.
    /// </summary>
    MessagingTransport? Transport { get; }
}
