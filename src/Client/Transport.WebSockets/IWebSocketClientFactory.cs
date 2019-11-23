using System.Net.WebSockets;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Transport
{
    /// <summary>
    /// A factory abstraction for a component that can create <see cref="ClientWebSocket"/>
    /// instances with custom configuration for a given logical name.
    /// </summary>
    public interface IWebSocketClientFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="WebSocketClient"/> instance using the
        /// configuration that corresponds to the logical name specified by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The logical name of the client to create.</param>
        /// <returns>A new <see cref="WebSocketClient"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// Each call to <see cref="CreateClient(string)"/> is guaranteed to return
        /// a new <see cref="WebSocketClient"/>
        /// instance. Callers may cache the returned <see cref="WebSocketClient"/>
        /// instance indefinitely or surround  its use in a <langword>using</langword>
        /// block to dispose it when desired.
        /// </para>
        /// <para>
        /// Callers are also free to mutate the returned <see cref="WebSocketClient"/>
        /// instance's public properties as desired.
        /// </para>
        /// </remarks>
        WebSocketClient CreateClient(string name);
    }
}
