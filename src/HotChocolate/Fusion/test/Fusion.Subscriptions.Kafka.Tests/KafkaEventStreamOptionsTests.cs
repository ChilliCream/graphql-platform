using System.Buffers;
using System.Threading.Channels;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaEventStreamOptionsTests
{
    [Theory]
    [InlineData(BoundedChannelFullMode.DropOldest, true, false)]
    [InlineData(BoundedChannelFullMode.DropNewest, true, false)]
    [InlineData(BoundedChannelFullMode.DropWrite, false, true)]
    public void CreateBoundedMessageChannel_Should_DisposeDroppedMessages_When_DropModeIsUsed(
        BoundedChannelFullMode fullMode,
        bool expectFirstDisposed,
        bool expectSecondDisposed)
    {
        // arrange
        using var firstOwner = new TrackingMemoryOwner(1);
        using var secondOwner = new TrackingMemoryOwner(1);
        var first = new EventMessage(firstOwner, 0..1, 0..0);
        var second = new EventMessage(secondOwner, 0..1, 0..0);
        var channel = KafkaEventStreamOptions.CreateBoundedMessageChannel(
            capacity: 1,
            fullMode);

        // act
        Assert.True(channel.Writer.TryWrite(first));
        Assert.True(channel.Writer.TryWrite(second));

        // assert
        Assert.Equal(expectFirstDisposed, firstOwner.IsDisposed);
        Assert.Equal(expectSecondDisposed, secondOwner.IsDisposed);
        Assert.True(channel.Reader.TryRead(out var remaining));
        remaining.Dispose();
    }

    private sealed class TrackingMemoryOwner : IMemoryOwner<byte>
    {
        private readonly byte[] _buffer;

        public TrackingMemoryOwner(int length)
        {
            _buffer = new byte[length];
        }

        public bool IsDisposed { get; private set; }

        public Memory<byte> Memory => _buffer;

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
