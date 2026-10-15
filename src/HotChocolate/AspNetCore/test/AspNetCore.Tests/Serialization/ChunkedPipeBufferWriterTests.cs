using System.IO.Pipelines;
using System.Text;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class ChunkedPipeBufferWriterTests
{
    private const int ChunkSize = 64 * 1024;

    [Fact]
    public async Task GetSpan_Should_BypassBuffer_When_SizeHintReachesDirectWriteThreshold()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        Fill(writer, 10, (byte)'a');

        // act
        const int size = ChunkedPipeBufferWriter.DirectWriteThreshold;
        writer.GetSpan(size)[..size].Fill((byte)'b');
        writer.Advance(size);
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        var expected = new byte[size + 10];
        expected.AsSpan(0, 10).Fill((byte)'a');
        expected.AsSpan(10).Fill((byte)'b');
        Assert.Equal(expected, output.ToArray());
        Assert.Equal(size, pipe.MaxSizeHint);
    }

    [Fact]
    public async Task GetSpan_Should_CopyBufferToPipe_When_SizeHintExceedsRemainingSpace()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        Fill(writer, ChunkSize - 10, (byte)'a');
        var spanCountBeforeOverflow = pipe.SpanCount;

        // act
        writer.GetSpan(20)[..20].Fill((byte)'b');
        writer.Advance(20);
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        var expected = new byte[ChunkSize + 10];
        expected.AsSpan(0, ChunkSize - 10).Fill((byte)'a');
        expected.AsSpan(ChunkSize - 10).Fill((byte)'b');
        Assert.Equal(expected, output.ToArray());
        Assert.Equal(0, spanCountBeforeOverflow);
        Assert.Equal(0, pipe.MaxSizeHint);
    }

    [Fact]
    public void Advance_Should_Throw_When_BytesExceedBuffer()
    {
        // arrange
        var pipe = PipeWriter.Create(Stream.Null);
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        writer.GetSpan(5);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => writer.Advance(ChunkSize + 1));

        // assert
        Assert.Equal("Cannot advance past the end of the buffer.", exception.Message);
    }

    [Fact]
    public void Advance_Should_Throw_When_BytesAreNegative()
    {
        // arrange
        var pipe = PipeWriter.Create(Stream.Null);
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        writer.GetSpan(5);

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(-1));

        // assert
        Assert.Equal("bytes", exception.ParamName);
    }

    [Fact]
    public async Task FlushAsync_Should_ReleaseBuffer_When_Called()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        var hasBufferBeforeWrite = writer.HasBuffer;
        Fill(writer, 5, (byte)'a');
        var hasBufferAfterWrite = writer.HasBuffer;

        // act
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        Assert.False(hasBufferBeforeWrite);
        Assert.True(hasBufferAfterWrite);
        Assert.False(writer.HasBuffer);
        Assert.Equal("aaaaa", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task Discard_Should_DropBufferedBytes_When_Called()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        Fill(writer, 3, (byte)'a');

        // act
        writer.Discard();
        var hasBufferAfterDiscard = writer.HasBuffer;
        Fill(writer, 3, (byte)'b');
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        Assert.False(hasBufferAfterDiscard);
        Assert.Equal("bbb", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task Discard_Should_KeepBytesCopiedToPipe_When_BufferOverflowed()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        Fill(writer, ChunkSize - 10, (byte)'a');
        // the overflowing write copies the buffered bytes to the pipe
        Fill(writer, 20, (byte)'b');

        // act
        writer.Discard();
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        var expected = new byte[ChunkSize - 10];
        expected.AsSpan().Fill((byte)'a');
        Assert.Equal(expected, output.ToArray());
    }

    [Fact]
    public async Task Dispose_Should_DiscardUnflushedBytes_When_Called()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var writer = ChunkedPipeBufferWriter.Rent(pipe);
        Fill(writer, 5, (byte)'a');

        // act
        writer.Dispose();
        await pipe.CompleteAsync();

        // assert
        Assert.Empty(output.ToArray());
    }

    [Fact]
    public void Rent_Should_ReuseDisposedWriter_When_RentedOnSameThread()
    {
        // arrange
        var pipe = PipeWriter.Create(Stream.Null);
        var first = ChunkedPipeBufferWriter.Rent(pipe);
        first.Dispose();

        // act
        using var second = ChunkedPipeBufferWriter.Rent(pipe);

        // assert
        Assert.Same(first, second);
    }

    /// <summary>
    /// Writes <paramref name="count"/> bytes of <paramref name="value"/> in writes of at most 1 KB.
    /// </summary>
    private static void Fill(ChunkedPipeBufferWriter writer, int count, byte value)
    {
        for (var written = 0; written < count; written += 1024)
        {
            var size = Math.Min(1024, count - written);
            writer.GetSpan(size)[..size].Fill(value);
            writer.Advance(size);
        }
    }
}
