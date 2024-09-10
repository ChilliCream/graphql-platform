using Xunit;

namespace HotChocolate.Utilities;

public class ArrayWriterTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperly()
    {
        // Arrange & Act
        using var writer = new ArrayWriter();

        // Assert
        Assert.NotNull(writer.GetInternalBuffer());
        Assert.Equal(0, writer.Length);
    }

    [Fact]
    public void GetWrittenMemory_ShouldReturnReadOnlyMemory()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        var memory = writer.GetWrittenMemory();

        // Assert
        Assert.Equal(0, memory.Length);
    }

    [Fact]
    public void GetWrittenSpan_ShouldReturnReadOnlySpan()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        var span = writer.GetWrittenSpan();

        // Assert
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void Advance_ShouldAdvanceCorrectly()
    {
        // Arrange
        using var writer = new ArrayWriter();
        writer.GetSpan(10);

        // Act
        writer.Advance(5);

        // Assert
        Assert.Equal(5, writer.Length);
    }

    [Fact]
    public void GetMemory_ShouldReturnMemoryWithCorrectSizeHint()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        var memory = writer.GetMemory(10);

        // Assert
        Assert.True(memory.Length >= 10);
    }

    [Fact]
    public void GetSpan_ShouldReturnSpanWithCorrectSizeHint()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        var span = writer.GetSpan(10);

        // Assert
        Assert.True(span.Length >= 10);
    }

    [Fact]
    public void Dispose_ShouldDisposeCorrectly()
    {
        // Arrange
        var writer = new ArrayWriter();

        // Act
        writer.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => writer.Advance(0));
    }

    [Fact]
    public void Advance_ShouldThrowWhenDisposed()
    {
        // Arrange
        var writer = new ArrayWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.Advance(0));
    }

    [Fact]
    public void Advance_ShouldThrowWhenNegativeCount()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(-1));
    }

    [Fact]
    public void Advance_ShouldThrowWhenCountGreaterThanCapacity()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => writer.Advance(writer.GetInternalBuffer().Length + 1));
    }

    [Fact]
    public void GetMemory_ShouldThrowWhenDisposed()
    {
        // Arrange
        var writer = new ArrayWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory());
    }

    [Fact]
    public void GetMemory_ShouldThrowWhenNegativeSizeHint()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(-1));
    }

    [Fact]
    public void GetSpan_ShouldThrowWhenDisposed()
    {
        // Arrange
        var writer = new ArrayWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan());
    }

    [Fact]
    public void GetSpan_ShouldThrowWhenNegativeSizeHint()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));
    }

    [Fact]
    public void WriteBytesToSpan_ShouldWriteCorrectly()
    {
        // Arrange
        using var writer = new ArrayWriter();
        var testData = new byte[] { 1, 2, 3, 4, };

        // Act
        var span = writer.GetSpan(4);
        testData.CopyTo(span);
        writer.Advance(4);

        // Assert
        Assert.Equal(4, writer.Length);
        var writtenSpan = writer.GetWrittenSpan();
        Assert.True(testData.SequenceEqual(writtenSpan.ToArray()));
    }

    [Fact]
    public void WriteBytesToMemory_ShouldWriteCorrectly()
    {
        // Arrange
        using var writer = new ArrayWriter();
        var testData = new byte[] { 1, 2, 3, 4, };

        // Act
        var memory = writer.GetMemory(4);
        testData.CopyTo(memory);
        writer.Advance(4);

        // Assert
        Assert.Equal(4, writer.Length);
        var writtenMemory = writer.GetWrittenMemory();
        Assert.True(testData.SequenceEqual(writtenMemory.ToArray()));
    }

    [Fact]
    public void WriteBytesExceedingInitialBufferSize_ShouldExpandAndWriteCorrectly()
    {
        // Arrange
        using var writer = new ArrayWriter();
        var testData = new byte[1024];

        for (var i = 0; i < testData.Length; i++)
        {
            testData[i] = (byte)(i % 256);
        }

        // Act
        for (var i = 0; i < testData.Length; i += 128)
        {
            var span = writer.GetSpan(128);
            testData.AsSpan(i, 128).CopyTo(span);
            writer.Advance(128);
        }

        // Assert
        Assert.Equal(1024, writer.Length);
        var writtenSpan = writer.GetWrittenSpan();
        Assert.True(testData.SequenceEqual(writtenSpan.ToArray()));
    }

    [Fact]
    public void ShouldAllocateSufficientMemory()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        // NB: ask for 0x3000 bytes because the initial 512 bytes buffer size is added
        // to request when doubling is insufficient and ArrayPool<byte> sizes are powers of 2
        writer.GetSpan (0x3000) ;
        writer.Advance (0x2000) ;
        writer.GetSpan (0x7000) ;
    }

    [Fact]
    public void ShouldResetCapacity()
    {
        // Arrange
        using var writer = new ArrayWriter();

        // Act
        writer.GetSpan(1000);
        writer.Advance(1000);
        writer.Reset();
        writer.GetSpan(2000);
        writer.Advance(2000);
    }
}
