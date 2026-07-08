namespace Mocha;

/// <summary>
/// Provides generated and user descriptor configuration during messaging setup.
/// </summary>
public sealed class MessagingConfigurationFeature(MessagingConfigurationContainer configurations)
{
    public MessagingConfigurationContainer Configurations { get; } = configurations;
}
