using System.Buffers;

namespace HotChocolate.Fusion.Transport.Sockets.Client;

/// <summary>
/// Configures the Fusion WebSocket client.
/// </summary>
public sealed class SocketClientOptions
{
    private int _maxOperationQueueBytes = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum number of payload bytes that one operation may queue.
    /// </summary>
    public int MaxOperationQueueBytes
    {
        get => _maxOperationQueueBytes;
        init
        {
            if (value <= 0)
            {
                throw ThrowHelper.MaxOperationQueueBytesOutOfRange(value);
            }

            _maxOperationQueueBytes = value;
        }
    }

    internal ArrayPool<byte> PayloadBufferPool { get; init; } = ArrayPool<byte>.Shared;
}
