using System.Buffers;
using Mocha.Features;
using Mocha.Utils;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A pooled feature that provides a reusable buffer for serializing message headers as JSON.
/// </summary>
internal sealed class JsonHeadersFeature : IPooledFeature
{
    private readonly PooledArrayWriter _writer = new();

    /// <summary>
    /// Gets the buffer writer used to serialize header JSON.
    /// </summary>
    public IBufferWriter<byte> Writer => _writer;

    /// <summary>
    /// Returns the serialized JSON bytes written so far.
    /// </summary>
    public ReadOnlyMemory<byte> GetWrittenBytes() => _writer.WrittenMemory;

    /// <inheritdoc />
    public void Initialize(object state)
    {
        _writer.Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        _writer.Reset();
    }
}
