namespace HotChocolate.Buffers;

public class ReadOnlyMemorySegmentTests
{
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
}
