using System.Buffers;
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
    public async Task GetSpan_Should_GrowPipeSizeHint_When_DirectWritesFillTheirSpans()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);

        // act
        WriteFullSpans(writer, 96 * 1024);
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await counting.CompleteAsync();

        // assert
        var (content, segments) = await ReadToEndAsync(pipe.Reader);
        Assert.Equal(CreatePattern(96 * 1024), content);
        Assert.Equal([2048, 4096, 8192, 16384, 16384, 16384, 16384, 16384], counting.SizeHints);
        Assert.Equal(8, segments);
    }

    [Fact]
    public async Task GetSpan_Should_ForwardSizeHint_When_DirectWriteLeavesRoomInItsSpan()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);

        // act
        for (var i = 0; i < 3; i++)
        {
            writer.GetSpan(ChunkedPipeBufferWriter.DirectWriteThreshold)[..1024].Fill((byte)'a');
            writer.Advance(1024);
        }

        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await counting.CompleteAsync();

        // assert
        var (content, segments) = await ReadToEndAsync(pipe.Reader);
        Assert.Equal(new string('a', 3 * 1024), Encoding.UTF8.GetString(content));
        Assert.Equal([2048, 2048, 2048], counting.SizeHints);
        Assert.Equal(1, segments);
    }

    [Fact]
    public void GetSpan_Should_ForwardCallerSizeHint_When_ItExceedsGrownSizeHint()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);
        WriteFullSpans(writer, 8 * 1024);

        // act
        var span = writer.GetSpan(ChunkedPipeBufferWriter.MaxPipeSizeHint * 2);

        // assert
        Assert.Equal([2048, 4096, 32768], counting.SizeHints);
        Assert.True(span.Length >= ChunkedPipeBufferWriter.MaxPipeSizeHint * 2);
    }

    [Fact]
    public void GetSpan_Should_ForwardSizeHint_When_BufferedBytesWereCopiedToPipe()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);
        WriteFullSpans(writer, 4 * 1024);
        Fill(writer, 10, (byte)'a');

        // act
        writer.GetSpan(ChunkedPipeBufferWriter.DirectWriteThreshold);

        // assert
        Assert.Equal([2048, 0, 2048], counting.SizeHints);
    }

    [Fact]
    public async Task FlushAsync_Should_ResetPipeSizeHint_When_Called()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);
        WriteFullSpans(writer, 32 * 1024);

        // act
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        WriteFullSpans(writer, 1024);

        // assert
        Assert.Equal([2048, 4096, 8192, 16384, 2048], counting.SizeHints);
    }

    [Fact]
    public async Task GetSpan_Should_ForwardSizeHint_When_RentedWithoutSpanGrowth()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        using var writer = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: false);

        // act
        WriteFullSpans(writer, 16 * 1024);
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await counting.CompleteAsync();

        // assert
        var (content, segments) = await ReadToEndAsync(pipe.Reader);
        Assert.Equal(CreatePattern(16 * 1024), content);
        Assert.Equal([2048, 2048, 2048, 2048], counting.SizeHints);
        Assert.Equal(4, segments);
    }

    [Fact]
    public void Advance_Should_Throw_When_BytesExceedBuffer()
    {
        // arrange
        var pipe = PipeWriter.Create(Stream.Null);
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        using var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        var writer = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
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
        var first = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);
        first.Dispose();

        // act
        using var second = ChunkedPipeBufferWriter.Rent(pipe, growPipeSpans: false);

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Rent_Should_ForwardSizeHint_When_WriterThatGrewSpansIsRentedWithoutSpanGrowth()
    {
        // arrange
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var counting = new CountingPipeWriter(pipe.Writer);
        var first = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: true);
        WriteFullSpans(first, 8 * 1024);
        first.Dispose();
        counting.SizeHints.Clear();

        // act
        using var second = ChunkedPipeBufferWriter.Rent(counting, growPipeSpans: false);
        WriteFullSpans(second, 8 * 1024);

        // assert
        Assert.Same(first, second);
        Assert.Equal([2048, 2048], counting.SizeHints);
    }

    /// <summary>
    /// Writes <paramref name="count"/> bytes of <see cref="CreatePattern"/> through spans requested
    /// with the direct write threshold, filling each span before requesting the next one.
    /// </summary>
    private static void WriteFullSpans(ChunkedPipeBufferWriter writer, int count)
    {
        var pattern = CreatePattern(count);

        for (var written = 0; written < count;)
        {
            var span = writer.GetSpan(ChunkedPipeBufferWriter.DirectWriteThreshold);
            var size = Math.Min(span.Length, count - written);
            pattern.AsSpan(written, size).CopyTo(span);
            writer.Advance(size);
            written += size;
        }
    }

    /// <summary>
    /// Creates <paramref name="count"/> bytes whose values repeat every 251 bytes.
    /// </summary>
    private static byte[] CreatePattern(int count)
    {
        var pattern = new byte[count];

        for (var i = 0; i < count; i++)
        {
            pattern[i] = (byte)(i % 251);
        }

        return pattern;
    }

    /// <summary>
    /// Reads <paramref name="reader"/> to its end and returns the bytes and the number of segments
    /// that held them.
    /// </summary>
    private static async Task<(byte[] Content, int Segments)> ReadToEndAsync(PipeReader reader)
    {
        while (true)
        {
            var result = await reader.ReadAsync(TestContext.Current.CancellationToken);
            var buffer = result.Buffer;

            if (result.IsCompleted)
            {
                var segments = 0;
                var position = buffer.Start;

                while (buffer.TryGet(ref position, out _))
                {
                    segments++;
                }

                var content = buffer.ToArray();
                reader.AdvanceTo(buffer.End);
                return (content, segments);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
        }
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
