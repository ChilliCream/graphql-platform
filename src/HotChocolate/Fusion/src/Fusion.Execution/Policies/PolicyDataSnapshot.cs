using System.Buffers;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Represents a materialized snapshot of the data document that backs policy evaluation.
/// </summary>
/// <remarks>
/// Ownership of the snapshot transfers to the subscriber that receives it. The subscriber disposes
/// the snapshot when it is no longer needed, which returns the pooled memory that backs
/// <see cref="Data"/>. A provider never disposes a snapshot it has delivered.
/// </remarks>
public sealed class PolicyDataSnapshot : IDisposable
{
    private readonly IMemoryOwner<byte> _owner;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyDataSnapshot"/>.
    /// </summary>
    /// <param name="data">The owner of the memory that backs the data document.</param>
    /// <param name="length">The number of bytes of <paramref name="data"/> that are in use.</param>
    public PolicyDataSnapshot(IMemoryOwner<byte> data, int length)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, data.Memory.Length);

        _owner = data;
        _length = length;
    }

    /// <summary>
    /// Gets the UTF-8 encoded JSON of the complete data document.
    /// </summary>
    public ReadOnlyMemory<byte> Data => _owner.Memory[.._length];

    /// <summary>
    /// Releases the pooled memory that backs the data document.
    /// </summary>
    public void Dispose() => _owner.Dispose();
}
