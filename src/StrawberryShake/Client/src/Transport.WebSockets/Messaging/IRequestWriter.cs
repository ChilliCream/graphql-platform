using System.Buffers;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a output sink into which bytes can be written.
/// Exposes the written buffer with the Body property
/// </summary>
public interface IRequestWriter
    : IBufferWriter<byte>
        , IDisposable
{
    /// <summary>
    /// The body of the buffered data
    /// </summary>
    ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    /// Resets the buffer so it can be reused
    /// </summary>
    void Reset();
}
