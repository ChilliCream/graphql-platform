using System.Buffers;

namespace HotChocolate.Buffers;

/// <summary>
/// Represents expandable memory that can be referenced, written to and be dismissed.
/// </summary>
public interface IWritableMemory : IBufferWriter<byte>, IMemoryOwner<byte>
{
    /// <summary>
    /// Gets the part of the memory that has been written to.
    /// </summary>
    ReadOnlySpan<byte> WrittenSpan { get; }

    /// <summary>
    /// Gets the part of the memory that has been written to.
    /// </summary>
    ReadOnlyMemory<byte> WrittenMemory { get; }
}
