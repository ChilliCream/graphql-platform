using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Represents the configuration options for the Mocha Mediator.
/// </summary>
public sealed class MediatorOptions
{
    /// <summary>
    /// Gets or sets the default service lifetime for handlers and behaviors.
    /// Default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;
}
