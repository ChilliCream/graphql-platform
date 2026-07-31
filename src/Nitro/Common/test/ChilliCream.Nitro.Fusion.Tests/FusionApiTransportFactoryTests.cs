using System.Buffers;
using ChilliCream.Nitro.Fusion.Transport;

namespace ChilliCream.Nitro.Fusion;

public sealed class FusionApiTransportFactoryTests
{
    [Fact]
    public async Task ReadSourceSchemaArchiveAsync_Should_RejectBody_When_DeclaredLengthIsTooLarge()
    {
        using var content = new ByteArrayContent([1]);
        content.Headers.ContentLength = 17;

        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => FusionApiTransportFactory.ReadSourceSchemaArchiveAsync(
                content,
                16,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The compressed Fusion source schema archive exceeds the maximum "
            + "allowed size of 16 bytes.",
            exception.Message);
    }

    [Fact]
    public async Task ReadSourceSchemaArchiveAsync_Should_RejectBody_When_ChunkedBodyIsTooLarge()
    {
        await using var stream = new NonSeekableReadStream(new byte[17]);
        using var content = new StreamContent(stream);
        var bufferPool = new TrackingArrayPool();
        Assert.Null(content.Headers.ContentLength);

        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => FusionApiTransportFactory.ReadSourceSchemaArchiveAsync(
                content,
                16,
                bufferPool,
                4,
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The compressed Fusion source schema archive exceeds the maximum "
            + "allowed size of 16 bytes.",
            exception.Message);
        AssertAllBuffersCleared(bufferPool);
    }

    [Fact]
    public async Task ReadSourceSchemaArchiveAsync_Should_Cancel_When_ReadIsCanceled()
    {
        await using var stream = new BlockingReadStream();
        using var content = new StreamContent(stream);
        using var cancellation = new CancellationTokenSource();
        var bufferPool = new TrackingArrayPool();
        var readTask = FusionApiTransportFactory.ReadSourceSchemaArchiveAsync(
            content,
            16,
            bufferPool,
            4,
            cancellation.Token);

        await stream.ReadStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readTask);
        AssertAllBuffersCleared(bufferPool);
    }

    [Fact]
    public async Task ReadSourceSchemaArchiveAsync_Should_ClearEveryRentedBuffer_When_BodyGrows()
    {
        byte[] expected = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        await using var stream = new NonSeekableReadStream(expected);
        using var content = new StreamContent(stream);
        var bufferPool = new TrackingArrayPool();

        var archive = await FusionApiTransportFactory.ReadSourceSchemaArchiveAsync(
            content,
            16,
            bufferPool,
            4,
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, archive);
        Assert.Equal(3, bufferPool.Rented.Count);
        AssertAllBuffersCleared(bufferPool);
    }

    private static void AssertAllBuffersCleared(TrackingArrayPool bufferPool)
    {
        Assert.Equal(bufferPool.Rented.Count, bufferPool.Returned.Count);
        Assert.All(bufferPool.Returned, returned => Assert.True(returned.ClearArray));
        Assert.All(
            bufferPool.Rented,
            buffer => Assert.Equal(new byte[buffer.Length], buffer));
    }

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content, writable: false);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class BlockingReadStream : Stream
    {
        public TaskCompletionSource ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            ReadStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
    }

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        public List<byte[]> Rented { get; } = [];

        public List<(byte[] Buffer, bool ClearArray)> Returned { get; } = [];

        public override byte[] Rent(int minimumLength)
        {
            var buffer = new byte[minimumLength];
            Rented.Add(buffer);
            return buffer;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (Returned.Any(returned => ReferenceEquals(returned.Buffer, array)))
            {
                throw new InvalidOperationException("The buffer was returned twice.");
            }

            Returned.Add((array, clearArray));

            if (clearArray)
            {
                array.AsSpan().Clear();
            }
        }
    }
}
