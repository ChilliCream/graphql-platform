using System.Buffers;
using System.IO.Pipelines;

namespace HotChocolate.AspNetCore.Formatters;

public sealed class ResponseBodyBufferTests
{
    [Fact]
    public async Task CopyTo_Should_CopyEveryByteInOrder_When_WritesCrossChunkBoundaries()
    {
        // arrange
        var expected = new ArrayBufferWriter<byte>();
        using var buffer = ResponseBodyBuffer.Rent();
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var value = 0;

        // writes of every size from 1 byte up to 4 KB, then one write larger than a chunk
        for (var size = 1; size <= 4096; size += 7)
        {
            Write(buffer, expected, size, ref value);
        }

        Write(buffer, expected, 1_200_000, ref value);
        Write(buffer, expected, 3, ref value);

        // act
        var copies = 1;

        while (buffer.CopyTo(pipe.Writer))
        {
            copies++;
        }

        await pipe.Writer.CompleteAsync();
        var actual = await ReadAllAsync(pipe.Reader);

        // assert
        Assert.Equal(expected.WrittenCount, buffer.Length);
        Assert.Equal(expected.WrittenSpan.ToArray(), actual);
        Assert.Equal(3, copies);
    }

    [Fact]
    public async Task FlushAsync_Should_ReturnCanceledResultOnce_When_CancelPendingFlushWasCalled()
    {
        // arrange
        using var buffer = ResponseBodyBuffer.Rent();

        // act
        buffer.CancelPendingFlush();
        var canceled = await buffer.FlushAsync(TestContext.Current.CancellationToken);
        var next = await buffer.FlushAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(canceled.IsCanceled);
        Assert.False(canceled.IsCompleted);
        Assert.False(next.IsCanceled);
        Assert.False(next.IsCompleted);
    }

    [Fact]
    public async Task FlushAsync_Should_KeepBufferedBytes_When_CalledBetweenWrites()
    {
        // arrange
        using var buffer = ResponseBodyBuffer.Rent();
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));

        // act
        buffer.Write("abc"u8);
        await buffer.FlushAsync(TestContext.Current.CancellationToken);
        buffer.Write("def"u8);
        buffer.CopyTo(pipe.Writer);
        await pipe.Writer.CompleteAsync();

        // assert
        Assert.Equal("abcdef"u8.ToArray(), await ReadAllAsync(pipe.Reader));
    }

    [Fact]
    public void GetSpan_Should_Throw_When_WriterWasCompleted()
    {
        // arrange
        using var buffer = ResponseBodyBuffer.Rent();
        buffer.Write("abc"u8);
        buffer.Complete();

        // act
        void Act() => buffer.GetSpan(1);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("Writing is not allowed after the writer was completed.", exception.Message);
        Assert.Equal(3, buffer.Length);
    }

    [Fact]
    public void Complete_Should_KeepException_When_CompletedWithException()
    {
        // arrange
        using var buffer = ResponseBodyBuffer.Rent();
        var error = new InvalidOperationException("boom");

        // act
        buffer.Complete(error);

        // assert
        Assert.Same(error, buffer.CompletionException);
    }

    [Fact]
    public void Advance_Should_Throw_When_CountExceedsSpan()
    {
        // arrange
        using var buffer = ResponseBodyBuffer.Rent();
        var length = buffer.GetSpan(1).Length;

        // act
        void Act() => buffer.Advance(length + 1);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("Cannot advance past the end of the buffer.", exception.Message);
    }

    private static void Write(
        ResponseBodyBuffer buffer,
        ArrayBufferWriter<byte> expected,
        int size,
        ref int value)
    {
        var span = buffer.GetSpan(size);
        var expectedSpan = expected.GetSpan(size);

        for (var i = 0; i < size; i++)
        {
            span[i] = expectedSpan[i] = (byte)value++;
        }

        buffer.Advance(size);
        expected.Advance(size);
    }

    private static async Task<byte[]> ReadAllAsync(PipeReader reader)
    {
        while (true)
        {
            var result = await reader.ReadAsync(TestContext.Current.CancellationToken);

            if (result.IsCompleted)
            {
                var bytes = result.Buffer.ToArray();
                reader.AdvanceTo(result.Buffer.End);
                await reader.CompleteAsync();
                return bytes;
            }

            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }
}
