using System.Buffers;
using System.Threading.Channels;
using HotChocolate.Fusion.Subscriptions;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

public sealed class AzureEventHubsEventStreamOptionsTests
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
        var channel = AzureEventHubsEventStreamOptions.CreateBoundedMessageChannel(
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

    [Fact]
    public void Provider_Should_Throw_When_SeedingQueryTimeoutNotPositive()
    {
        // arrange
        void Mutate(AzureEventHubsEventStreamOptions options)
        {
            options.SeedingQueryTimeout = TimeSpan.Zero;
        }

        // act
        void Act() => CreateProvider(Mutate);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Provider_Should_Throw_When_SeedingDeadlineNotPositive()
    {
        // arrange
        void Mutate(AzureEventHubsEventStreamOptions options)
        {
            options.SeedingDeadline = TimeSpan.Zero;
        }

        // act
        void Act() => CreateProvider(Mutate);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Provider_Should_Throw_When_SeedingDeadlineBelowQueryTimeout()
    {
        // arrange
        void Mutate(AzureEventHubsEventStreamOptions options)
        {
            options.SeedingQueryTimeout = TimeSpan.FromSeconds(2);
            options.SeedingDeadline = TimeSpan.FromSeconds(1);
        }

        // act
        void Act() => CreateProvider(Mutate);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Provider_Should_Throw_When_PartitionDiscoveryIntervalNotPositive()
    {
        // arrange
        void Mutate(AzureEventHubsEventStreamOptions options)
        {
            options.PartitionDiscoveryInterval = TimeSpan.Zero;
        }

        // act
        void Act() => CreateProvider(Mutate);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    private static void CreateProvider(Action<AzureEventHubsEventStreamOptions> mutate)
    {
        var options = new AzureEventHubsEventStreamOptions
        {
            ConnectionString =
                "Endpoint=sb://x/;SharedAccessKeyName=k;SharedAccessKey=v;UseDevelopmentEmulator=true;",
            ConsumerGroup = "$Default",
            MaximumWaitTime = TimeSpan.FromSeconds(1)
        };
        mutate(options);
        var monitor = new FakeOptionsMonitor<AzureEventHubsEventStreamOptions>(options);
        _ = new AzureEventHubsEventStreamBrokerProvider("azure", monitor);
    }

    private sealed class FakeOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable OnChange(Action<T, string?> listener)
            => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
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
