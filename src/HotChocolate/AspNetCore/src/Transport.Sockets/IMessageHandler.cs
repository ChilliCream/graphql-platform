using System.Buffers;

namespace HotChocolate.Transport.Sockets;

/// <summary>
/// The message handler processes the incoming socket messages.
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Is invoked for every incoming message by the <see cref="MessageProcessor"/>.
    /// </summary>
    /// <param name="message">
    /// The client message.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    ValueTask OnReceiveAsync(
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);
}
