using System.Net.WebSockets;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// A factory abstraction for a component that can create <see cref="ClientWebSocket"/>
/// instances with custom configuration for a given logical name.
/// </summary>
public interface ISocketClientFactory
{
    /// <summary>
    /// Creates and configures an <see cref="ISocketClient"/> instance using the
    /// configuration that corresponds to the logical name specified by <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The logical name of the client to create.</param>
    /// <returns>A new <see cref="ISocketClient"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Each call to <see cref="CreateClient(string)"/> is guaranteed to return
    /// a new <see cref="ISocketClient"/>
    /// instance. Callers may cache the returned <see cref="ISocketClient"/>
    /// instance indefinitely or surround  its use in a <langword>using</langword>
    /// block to dispose it when desired.
    /// </para>
    /// <para>
    /// Callers are also free to mutate the returned <see cref="ISocketClient"/>
    /// instance's public properties as desired.
    /// </para>
    /// </remarks>
    ISocketClient CreateClient(string name);
}
