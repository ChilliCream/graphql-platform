using System.Buffers;
using System.Runtime.InteropServices;

namespace HotChocolate.Buffers;

/// <summary>
/// An <see cref="IMemoryOwner{T}"/> over memory whose lifetime the caller manages.
/// </summary>
public sealed class ArrayMemoryOwner : IMemoryOwner<byte>
{
    private readonly ReadOnlyMemory<byte> _memory;

    /// <summary>
    /// Initializes a new instance of <see cref="ArrayMemoryOwner"/>.
    /// </summary>
    /// <param name="memory">
    /// The memory this owner exposes, which must stay valid for as long as this owner is used.
    /// </param>
    public ArrayMemoryOwner(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    /// <summary>
    /// Gets the memory this owner exposes.
    /// </summary>
    public Memory<byte> Memory => MemoryMarshal.AsMemory(_memory);

    /// <summary>
    /// Does nothing. The memory is not released by this owner.
    /// </summary>
    public void Dispose()
    {
    }
}
