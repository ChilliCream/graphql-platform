using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Provides access to the host-level service collection and bus name for registering services
/// during host configuration.
/// </summary>
public interface IMessageBusHostBuilder
{
    /// <summary>
    /// Gets the logical name of the message bus instance being configured.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the host-level service collection for registering dependencies required by the message bus.
    /// </summary>
    IServiceCollection Services { get; }
}
