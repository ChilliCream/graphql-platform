using System.Buffers;
using Mocha.Utils;

namespace Mocha.Tests;

public class PooledBufferAllocatorTests
{
    [Fact]
    public void GetMemory_Should_ReturnMemoryOfRequestedSize()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 1024);

        // act
        var memory = allocator.GetMemory(100);

        // assert
        Assert.Equal(100, memory.Length);
    }

    [Fact]
    public void GetMemory_Should_ReturnWritableMemory()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 64);

        // act
        var memory = allocator.GetMemory(4);
        memory.Span[0] = 0xDE;
        memory.Span[1] = 0xAD;
        memory.Span[2] = 0xBE;
        memory.Span[3] = 0xEF;

        // assert
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, memory.ToArray());
    }

    [Fact]
    public void GetMemory_Should_ReturnNonOverlappingSlices()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 1024);

        // act
        var first = allocator.GetMemory(50);
        var second = allocator.GetMemory(50);
        first.Span.Fill(0xAA);
        second.Span.Fill(0xBB);

        // assert
        Assert.True(first.Span.IndexOf((byte)0xBB) == -1, "First slice was corrupted by second write");
        Assert.True(second.Span.IndexOf((byte)0xAA) == -1, "Second slice was corrupted by first write");
    }

    [Fact]
    public void GetMemory_Should_PackSmallAllocationsIntoSingleBuffer()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 1024);

        // act
        allocator.GetMemory(100);
        allocator.GetMemory(100);
        allocator.GetMemory(100);

        // assert
        Assert.Equal(1, pool.RentCount);
        Assert.Equal(1, allocator.BufferCount);
    }

    [Fact]
    public void GetMemory_Should_RentNewBuffer_When_CurrentBufferExhausted()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act
        allocator.GetMemory(60); // 60 used, 40 remaining
        allocator.GetMemory(50); // 50 > 40 remaining → new buffer

        // assert
        Assert.Equal(2, pool.RentCount);
    }

    [Fact]
    public void GetMemory_Should_FillBufferExactlyThenRentNew()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act
        allocator.GetMemory(100); // fill exactly to capacity
        allocator.GetMemory(1); // must rent a new buffer

        // assert
        Assert.Equal(2, pool.RentCount);
    }

    [Fact]
    public void GetMemory_Should_RentLargerBuffer_When_SizeExceedsMinBufferSize()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 64);

        // act
        var memory = allocator.GetMemory(1024);

        // assert
        Assert.Equal(1024, memory.Length);
        Assert.Equal(1, pool.RentCount);
        Assert.True(pool.Rented[0].Length >= 1024);
    }

    [Fact]
    public void GetMemory_Should_HandleSingleLargeAllocation()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 64);

        // act
        var memory = allocator.GetMemory(10_000);

        // assert
        Assert.Equal(10_000, memory.Length);
        Assert.Equal(1, allocator.BufferCount);
    }

    [Fact]
    public void GetMemory_Should_PreserveDataAcrossBufferBoundaries()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act
        var first = allocator.GetMemory(80);
        first.Span.Fill(0x11);
        var second = allocator.GetMemory(80); // 80 > 20 remaining → new buffer
        second.Span.Fill(0x22);

        // assert
        Assert.True(first.Span.ToArray().All(b => b == 0x11));
        Assert.True(second.Span.ToArray().All(b => b == 0x22));
        Assert.Equal(2, pool.RentCount);
    }

    [Fact]
    public void GetMemory_Should_HandleManySmallAllocations()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 1024);
        var slices = new List<Memory<byte>>();

        // act - 100 x 10 = 1000 bytes, fits in one 1024-byte buffer
        for (var i = 0; i < 100; i++)
        {
            var slice = allocator.GetMemory(10);
            slice.Span.Fill((byte)(i % 256));
            slices.Add(slice);
        }

        // assert
        for (var i = 0; i < 100; i++)
        {
            var expected = (byte)(i % 256);
            Assert.True(slices[i].Span.ToArray().All(b => b == expected),
                $"Slice {i} was corrupted");
        }

        Assert.Equal(1, pool.RentCount);
    }

    [Fact]
    public void Dispose_Should_ReturnAllRentedBuffers()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);
        allocator.GetMemory(100); // buffer 1
        allocator.GetMemory(100); // buffer 2
        allocator.GetMemory(100); // buffer 3

        // act
        allocator.Dispose();

        // assert
        Assert.Equal(3, pool.ReturnCount);
        Assert.Equal(0, pool.Outstanding);
        for (var i = 0; i < 3; i++)
        {
            Assert.Contains(pool.Rented[i], pool.Returned);
        }
    }

    [Fact]
    public void Dispose_Should_BeIdempotent()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);
        allocator.GetMemory(50);

        // act
        allocator.Dispose();
        allocator.Dispose();

        // assert
        Assert.Equal(1, pool.ReturnCount);
    }

    [Fact]
    public void Dispose_Should_ReturnNothing_When_NothingAllocated()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act
        allocator.Dispose();

        // assert
        Assert.Equal(0, pool.RentCount);
        Assert.Equal(0, pool.ReturnCount);
    }

    [Fact]
    public void GetMemory_Should_ThrowObjectDisposedException_When_Disposed()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);
        allocator.Dispose();

        // act & assert
        Assert.Throws<ObjectDisposedException>(() => allocator.GetMemory(10));
    }

    [Fact]
    public void GetMemory_Should_ThrowArgumentOutOfRangeException_When_SizeIsZero()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.GetMemory(0));
    }

    [Fact]
    public void GetMemory_Should_ThrowArgumentOutOfRangeException_When_SizeIsNegative()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.GetMemory(-1));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_MinBufferSizeIsZero()
    {
        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PooledBufferAllocator(ArrayPool<byte>.Shared, 0));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_MinBufferSizeIsNegative()
    {
        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PooledBufferAllocator(ArrayPool<byte>.Shared, -1));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_PoolIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(
            () => new PooledBufferAllocator(null!, 100));
    }

    [Fact]
    public void BufferCount_Should_BeZero_When_NothingAllocated()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 100);

        // assert
        Assert.Equal(0, allocator.BufferCount);
    }

    [Fact]
    public void BufferCount_Should_MatchRentCount()
    {
        // arrange
        var pool = new TrackingArrayPool();
        using var allocator = new PooledBufferAllocator(pool, minBufferSize: 50);

        // act & assert
        allocator.GetMemory(50);
        Assert.Equal(1, allocator.BufferCount);

        allocator.GetMemory(50);
        Assert.Equal(2, allocator.BufferCount);

        allocator.GetMemory(50);
        Assert.Equal(3, allocator.BufferCount);

        Assert.Equal(pool.RentCount, allocator.BufferCount);
    }

    [Fact]
    public void DefaultConstructor_Should_UseSharedPool()
    {
        // arrange
        using var allocator = new PooledBufferAllocator();

        // act
        var memory = allocator.GetMemory(100);

        // assert
        Assert.Equal(100, memory.Length);
    }

    [Fact]
    public void MinBufferSizeConstructor_Should_UseSharedPool()
    {
        // arrange
        using var allocator = new PooledBufferAllocator(minBufferSize: 512);

        // act
        var memory = allocator.GetMemory(100);

        // assert
        Assert.Equal(100, memory.Length);
    }

    // A tracking pool that records every Rent/Return call with exact array references.
    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        private readonly List<byte[]> _rented = [];
        private readonly List<byte[]> _returned = [];

        public IReadOnlyList<byte[]> Rented => _rented;
        public IReadOnlyList<byte[]> Returned => _returned;
        public int RentCount => _rented.Count;
        public int ReturnCount => _returned.Count;
        public int Outstanding => _rented.Count - _returned.Count;

        public override byte[] Rent(int minimumLength)
        {
            // Return exact-size arrays so tests can reason about capacity precisely.
            var buffer = new byte[minimumLength];
            _rented.Add(buffer);
            return buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            _returned.Add(array);
        }
    }
}
