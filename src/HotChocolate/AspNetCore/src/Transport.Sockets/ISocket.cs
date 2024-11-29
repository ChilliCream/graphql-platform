using System.Buffers;

namespace HotChocolate.Transport.Sockets;

/// <summary>
/// Represents a socket to the <see cref="MessagePipeline"/>.
/// </summary>
public interface ISocket
{
    /// <summary>
    /// Defines if the connection is closed.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Reads a message from a socket and writes them to the specified <paramref name="writer" />.
    /// </summary>
    /// <param name="writer">
    /// The writer to which the message is written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a boolean that signals if the read was successful.
    /// </returns>
    Task<bool> ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken = default);
}
