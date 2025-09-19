namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a factory for <see cref="ISocketProtocol"/>
/// </summary>
public interface ISocketProtocolFactory
{
    /// <summary>
    /// The name of the protocol this factory can create
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// Creates a <see cref="ISocketProtocol"/>
    /// </summary>
    /// <param name="socketClient">The client where the socket protocol uses</param>
    /// <returns>A socket protocol</returns>
    ISocketProtocol Create(ISocketClient socketClient);
}
