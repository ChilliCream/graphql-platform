namespace HotChocolate.Transport.Http;

public class ReadTimeoutStreamTests
{
    [Fact]
    public async Task ReadAsync_Should_ThrowTimeoutException_When_ReadStallsLongerThanTimeout()
    {
        // arrange
        await using var inner = new StallingStream();
        await using var stream = new ReadTimeoutStream(inner, TimeSpan.FromMilliseconds(100));
        var buffer = new byte[16];

        // act
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => stream.ReadAsync(buffer, TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.Equal(
            "No data (including keep-alive messages) was received on the response stream "
            + "within the configured read timeout of 0.1 seconds.",
            exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowOperationCanceledException_When_CallerCancels()
    {
        // arrange
        await using var inner = new StallingStream();
        await using var stream = new ReadTimeoutStream(inner, TimeSpan.FromSeconds(30));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        var buffer = new byte[16];

        // act
        var exception = await Record.ExceptionAsync(
            () => stream.ReadAsync(buffer, cts.Token).AsTask());

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task ReadAsync_Should_NotTimeOut_When_ConsumerPausesBetweenReads()
    {
        // arrange
        // both reads are served promptly; only the consumer pauses for longer than the timeout
        await using var inner = new MemoryStream([1, 2, 3, 4, 5, 6]);
        await using var stream = new ReadTimeoutStream(inner, TimeSpan.FromMilliseconds(100));
        var buffer = new byte[3];

        // act
        var first = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(400), TestContext.Current.CancellationToken);
        var second = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, first);
        Assert.Equal(3, second);
        Assert.Equal(new byte[] { 4, 5, 6 }, buffer);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-10_000_000L)]
    [InlineData(long.MaxValue)]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_TimeoutIsOutOfRange(long ticks)
    {
        // arrange
        using var inner = new MemoryStream();

        // act
        var exception = Record.Exception(() => new ReadTimeoutStream(inner, TimeSpan.FromTicks(ticks)));

        // assert
        var argumentException = Assert.IsType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("timeout", argumentException.ParamName);
    }

    // A read-only stream that never completes a read until that read is cancelled.
    private sealed class StallingStream : Stream
    {
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await _never.Task.WaitAsync(cancellationToken);
            return 0;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
