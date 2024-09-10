using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// Builder for a <see cref="IInMemoryClientBuilder"/>
/// </summary>
internal class DefaultInMemoryClientBuilder
    : IInMemoryClientBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultInMemoryClientFactory"/>
    /// </summary>
    /// <param name="services">The service collection of the application</param>
    /// <param name="name">The name of the websocket</param>
    public DefaultInMemoryClientBuilder(IServiceCollection services, string name)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
