namespace Mocha;

/// <summary>
/// A convention that applies cross-cutting configuration to <see cref="MessageTypeConfiguration"/> during bus setup.
/// </summary>
public interface IMessageTypeConfigurationConvention : IConfigurationConvention<MessageTypeConfiguration>;
