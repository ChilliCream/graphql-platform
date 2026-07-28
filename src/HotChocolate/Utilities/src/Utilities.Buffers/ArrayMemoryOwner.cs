using System.Buffers;
using System.Runtime.InteropServices;

namespace HotChocolate.Buffers;

/// <summary>
/// A memory owner over memory whose lifetime is managed elsewhere.
/// </summary>
public sealed class ArrayMemoryOwner : IMemoryOwner<byte>
{
    private readonly ReadOnlyMemory<byte> _memory;

    /// <summary>
    /// Initializes a new instance of <see cref="ArrayMemoryOwner"/>.
    /// </summary>
    /// <param name="memory">
    /// The memory that is exposed by this owner.
    /// </param>
    public ArrayMemoryOwner(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    /// <summary>
    /// Gets the memory that is exposed by this owner.
    /// </summary>
    public Memory<byte> Memory => MemoryMarshal.AsMemory(_memory);

    /// <summary>
    /// Does nothing as the memory is not owned by this instance.
    /// </summary>
    public void Dispose()
    {
    }
}
