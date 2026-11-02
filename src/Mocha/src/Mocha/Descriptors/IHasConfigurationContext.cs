namespace Mocha;

/// <summary>
/// Indicates that a descriptor or extension has access to the messaging configuration context.
/// </summary>
public interface IHasConfigurationContext
{
    /// <summary>
    /// The descriptor context.
    /// </summary>
    IMessagingConfigurationContext Context { get; }
}
