using System.IO.Pipelines;
using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class ChunkedPipeBufferWriterTests
{
    private const int ChunkSize = 64 * 1024;

    [Fact]
    public async Task GetSpan_Should_WriteToPipe_When_SizeHintReachesDirectWriteThreshold()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        writer.GetSpan(10)[..10].Fill((byte)'a');
        writer.Advance(10);

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
    public async Task GetSpan_Should_CopyChunkToPipe_When_SizeHintExceedsRemainingSpace()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = new CountingPipeWriter(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);

        for (var written = 0; written < ChunkSize - 10; written += 1024)
        {
            var size = Math.Min(1024, ChunkSize - 10 - written);
            writer.GetSpan(size)[..size].Fill((byte)'a');
            writer.Advance(size);
        }

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
    public async Task FlushAsync_Should_ReleaseBuffer_When_Called()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        using var writer = ChunkedPipeBufferWriter.Rent(pipe);
        var hasBufferBeforeWrite = writer.HasBuffer;
        writer.GetSpan(5)[..5].Fill((byte)'a');
        writer.Advance(5);
        var hasBufferAfterWrite = writer.HasBuffer;

        // act
        await writer.FlushAsync(TestContext.Current.CancellationToken);
        await pipe.CompleteAsync();

        // assert
        Snapshot.Create()
            .Add(
                new
                {
                    HasBufferBeforeWrite = hasBufferBeforeWrite,
                    HasBufferAfterWrite = hasBufferAfterWrite,
                    HasBufferAfterFlush = writer.HasBuffer,
                    Output = System.Text.Encoding.UTF8.GetString(output.ToArray())
                },
                "Writer State")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Dispose_Should_DiscardUnflushedBytes_When_Called()
    {
        // arrange
        await using var output = new MemoryStream();
        var pipe = PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true));
        var writer = ChunkedPipeBufferWriter.Rent(pipe);
        writer.GetSpan(5)[..5].Fill((byte)'a');
        writer.Advance(5);

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
}
