namespace Mocha.Transport.Postgres.Tests.Topology;

public class PostgresMessageTypeExtensionTests
{
    [Fact]
    public void ToPostgresQueue_Should_CreateQueueUri_When_DefaultSchema()
    {
        // arrange
        var descriptor = new TestOutboundRouteDescriptor();

        // act
        descriptor.ToPostgresQueue("my-queue");

        // assert
        Assert.NotNull(descriptor.DestinationUri);
        Assert.Equal("postgres", descriptor.DestinationUri!.Scheme);
        Assert.Contains("q/my-queue", descriptor.DestinationUri.OriginalString);
    }

    [Fact]
    public void ToPostgresQueue_Should_CreateQueueUri_When_CustomSchema()
    {
        // arrange
        var descriptor = new TestOutboundRouteDescriptor();

        // act
        descriptor.ToPostgresQueue("custom", "my-queue");

        // assert
        Assert.NotNull(descriptor.DestinationUri);
        Assert.Equal("custom", descriptor.DestinationUri!.Scheme);
        Assert.Contains("q/my-queue", descriptor.DestinationUri.OriginalString);
    }

    [Fact]
    public void ToPostgresTopic_Should_CreateTopicUri_When_DefaultSchema()
    {
        // arrange
        var descriptor = new TestOutboundRouteDescriptor();

        // act
        descriptor.ToPostgresTopic("my-topic");

        // assert
        Assert.NotNull(descriptor.DestinationUri);
        Assert.Equal("postgres", descriptor.DestinationUri!.Scheme);
        Assert.Contains("t/my-topic", descriptor.DestinationUri.OriginalString);
    }

    [Fact]
    public void ToPostgresTopic_Should_CreateTopicUri_When_CustomSchema()
    {
        // arrange
        var descriptor = new TestOutboundRouteDescriptor();

        // act
        descriptor.ToPostgresTopic("custom", "my-topic");

        // assert
        Assert.NotNull(descriptor.DestinationUri);
        Assert.Equal("custom", descriptor.DestinationUri!.Scheme);
        Assert.Contains("t/my-topic", descriptor.DestinationUri.OriginalString);
    }

    private sealed class TestOutboundRouteDescriptor : IOutboundRouteDescriptor
    {
        public Uri? DestinationUri { get; private set; }

        public IOutboundRouteDescriptor Destination(Uri address)
        {
            DestinationUri = address;
            return this;
        }
    }
}
