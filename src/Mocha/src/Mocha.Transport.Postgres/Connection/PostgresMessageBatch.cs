using Mocha.Utils;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A batch of messages read from PostgreSQL that packs message data into pooled buffers
/// via a <see cref="PooledBufferAllocator"/>.
/// </summary>
public sealed class PostgresMessageBatch : IDisposable
{
    private readonly PooledBufferAllocator _allocator = new();

    /// <summary>
    /// Gets the messages in this batch.
    /// </summary>
    public List<PostgresMessageItem> Messages { get; } = [];

    /// <summary>
    /// Gets the number of messages in this batch.
    /// </summary>
    public int Count => Messages.Count;

    /// <summary>
    /// Returns a writable <see cref="Memory{T}"/> slice of the requested size
    /// from the underlying pooled buffer allocator.
    /// </summary>
    public Memory<byte> GetMemory(int size) => _allocator.GetMemory(size);

    /// <summary>
    /// Returns all rented buffers back to the pool.
    /// </summary>
    public void Dispose() => _allocator.Dispose();
}
