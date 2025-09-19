using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Builder for a <see cref="IWebSocketClientBuilder"/>
/// </summary>
internal class DefaultWebSocketClientBuilder : IWebSocketClientBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultSocketClientFactory"/>
    /// </summary>
    /// <param name="services">The service collection of the application</param>
    /// <param name="name">The name of the websocket</param>
    public DefaultWebSocketClientBuilder(IServiceCollection services, string name)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
