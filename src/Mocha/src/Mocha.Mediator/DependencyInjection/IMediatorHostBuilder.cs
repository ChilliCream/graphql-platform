using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Provides access to the host-level service collection and mediator name for registering services
/// during host configuration.
/// </summary>
public interface IMediatorHostBuilder
{
    /// <summary>
    /// Gets the logical name of the mediator instance being configured.
    /// An empty string represents the default (unnamed) mediator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the host-level service collection for registering dependencies required by the mediator.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the mediator options being configured.
    /// </summary>
    MediatorOptions Options { get; }
}
