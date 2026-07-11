namespace Mocha;

/// <summary>
/// A convention that applies cross-cutting configuration to <see cref="ReceiveEndpointConfiguration"/> during bus setup.
/// </summary>
public interface IReceiveEndpointConvention : IConfigurationConvention<ReceiveEndpointConfiguration>;
