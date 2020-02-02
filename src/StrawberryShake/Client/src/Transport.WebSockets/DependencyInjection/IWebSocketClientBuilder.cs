using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// A builder for configuring named <see cref="IWebSocketClient"/>
    /// instances returned by <see cref="IWebSocketClientFactory"/>.
    /// </summary>
    public interface IWebSocketClientBuilder
    {
        /// <summary>
        /// Gets the name of the client configured by this builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
