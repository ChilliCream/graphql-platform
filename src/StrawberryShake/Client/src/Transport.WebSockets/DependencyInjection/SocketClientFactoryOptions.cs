namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Options of a <see cref="ISocketClient"/>
/// </summary>
public class SocketClientFactoryOptions
{
    /// <summary>
    /// Gets a list of operations used to configure an <see cref="ISocketClient"/>.
    /// </summary>
    public IList<Action<ISocketClient>> SocketClientActions { get; } =
        new List<Action<ISocketClient>>();
}
