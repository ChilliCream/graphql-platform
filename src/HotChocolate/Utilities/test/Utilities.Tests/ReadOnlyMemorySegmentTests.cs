using System.Buffers;
using System.Runtime.InteropServices;

namespace HotChocolate.Buffers;

public class ReadOnlyMemorySegmentTests
{
    [Fact]
    public void Span_Should_ReturnEntireArray_When_ConstructedFromArray()
    {
        // arrange
        byte[] buffer = [1, 2, 3, 4];

        // act
        var segment = new ReadOnlyMemorySegment(buffer);

        // assert
        Assert.False(segment.IsEmpty);
        Assert.Equal(buffer.Length, segment.Length);
        Assert.Equal(buffer, segment.Span.ToArray());
        Assert.Equal((ReadOnlyMemory<byte>)buffer, segment.Memory);
    }

    [Fact]
    public void IsEmpty_Should_BeTrue_When_ArrayIsNull()
    {
        // arrange
        byte[]? buffer = null;

        // act
        var segment = new ReadOnlyMemorySegment(buffer);

        // assert
        Assert.True(segment.IsEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Span.ToArray());
        Assert.Equal(default, segment.Memory);
    }

    [Fact]
    public void IsEmpty_Should_BeFalse_When_ArrayHasNoElements()
    {
        // arrange
        var buffer = Array.Empty<byte>();

        // act
        var segment = new ReadOnlyMemorySegment(buffer);

        // assert
        Assert.False(segment.IsEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Span.ToArray());
        Assert.Equal((ReadOnlyMemory<byte>)buffer, segment.Memory);
    }

    [Fact]
    public void Memory_Should_PreservePinnedSlice_When_ConstructedFromPinnedMemory()
    {
        // arrange
        var buffer = GC.AllocateArray<byte>(4, pinned: true);
        buffer[1] = 2;
        buffer[2] = 3;
        ReadOnlyMemory<byte> memory = MemoryMarshal.CreateFromPinnedArray(buffer, 1, 2);

        // act
        var segment = new ReadOnlyMemorySegment(memory);

        // assert
        Assert.Equal(memory, segment.Memory);
        Assert.Equal(2, segment.Length);
        Assert.Equal([2, 3], segment.Span.ToArray());
    }

    [Fact]
    public void Span_Should_ResolveCurrentMemory_When_OwnerMemoryChanges()
    {
        // arrange
        using var owner = new MutableMemoryOwner([1, 2, 3, 4]);
        var segment = new ReadOnlyMemorySegment(owner, 1, 2);

        // act
        owner.Memory = new byte[] { 5, 6, 7, 8 };

        // assert
        Assert.Equal(2, segment.Length);
        Assert.Equal([6, 7], segment.Span.ToArray());
        Assert.Equal([6, 7], segment.Memory.ToArray());
    }

    [Fact]
    public void IsEmpty_Should_BeFalse_When_OwnerSliceHasNoElements()
    {
        // arrange
        using var owner = new ArrayMemoryOwner(new byte[] { 1, 2, 3, 4 });

        // act
        var segment = new ReadOnlyMemorySegment(owner, 2, 0);

        // assert
        Assert.False(segment.IsEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Span.ToArray());
        Assert.Equal(owner.Memory.Slice(2, 0), segment.Memory);
    }

    [Fact]
    public void Memory_Should_PreserveSlice_When_ConstructedFromMemoryManager()
    {
        // arrange
        using var manager = new TestMemoryManager([1, 2, 3, 4]);
        ReadOnlyMemory<byte> memory = manager.Memory.Slice(1, 2);

        // act
        var segment = new ReadOnlyMemorySegment(memory);

        // assert
        Assert.Equal(memory, segment.Memory);
        Assert.Equal(2, segment.Length);
        Assert.Equal([2, 3], segment.Span.ToArray());
    }

    [Fact]
    public void Span_Should_ReturnCarvedBytes_When_StartIsNonZero()
    {
        // arrange
        var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // act
        var segment = new ReadOnlyMemorySegment(buffer.AsMemory(3, 4));

        // assert
        Assert.Equal(4, segment.Length);
        Assert.Equal([4, 5, 6, 7], segment.Span.ToArray());
        Assert.Equal([4, 5, 6, 7], segment.Memory.ToArray());
    }

    [Fact]
    public void Span_Should_ReturnCarvedBytes_When_SegmentEndsAtBufferEnd()
    {
        // arrange
        var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // act
        var segment = new ReadOnlyMemorySegment(buffer.AsMemory(5, 3));

        // assert
        Assert.Equal(3, segment.Length);
        Assert.Equal([6, 7, 8], segment.Span.ToArray());
        Assert.Equal([6, 7, 8], segment.Memory.ToArray());
    }

    [Fact]
    public void Span_Should_ReturnSameBytesAsMemoryCtor_When_ConstructedFromMemoryOwner()
    {
        // arrange
        var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var owner = new ArrayMemoryOwner(buffer);

        // act
        var fromOwner = new ReadOnlyMemorySegment(owner, 3, 4);
        var fromMemory = new ReadOnlyMemorySegment(buffer.AsMemory(3, 4));

        // assert
        Assert.Equal(fromMemory.Length, fromOwner.Length);
        Assert.Equal(fromMemory.Span.ToArray(), fromOwner.Span.ToArray());
        Assert.Equal(fromMemory.Memory.ToArray(), fromOwner.Memory.ToArray());
    }

    [Fact]
    public void IsEmpty_Should_BeTrue_When_SegmentIsDefault()
    {
        // arrange
        ReadOnlyMemorySegment segment = default;

        // act
        var isEmpty = segment.IsEmpty;

        // assert
        Assert.True(isEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Span.ToArray());
        Assert.True(segment.Memory.IsEmpty);
    }

    [Fact]
    public void IsEmpty_Should_BeFalse_When_SegmentCarvesZeroBytesFromBuffer()
    {
        // arrange
        var buffer = new byte[] { 1, 2, 3, 4 };

        // act
        var segment = new ReadOnlyMemorySegment(buffer.AsMemory(2, 0));

        // assert
        Assert.False(segment.IsEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Span.ToArray());
    }

    private sealed class MutableMemoryOwner(byte[] buffer) : IMemoryOwner<byte>
    {
        public Memory<byte> Memory { get; set; } = buffer;

        public void Dispose()
        {
        }
    }

    private sealed class TestMemoryManager(byte[] buffer) : MemoryManager<byte>
    {
        public override Span<byte> GetSpan() => buffer;

        public override MemoryHandle Pin(int elementIndex = 0)
            => buffer.AsMemory(elementIndex).Pin();

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
