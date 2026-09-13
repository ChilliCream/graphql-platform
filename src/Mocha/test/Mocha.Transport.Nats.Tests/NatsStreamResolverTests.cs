using Moq;
using NATS.Client.JetStream;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsStreamResolverTests
{
    private static INatsJSContext JetStreamReturning(params string[] streamNames)
    {
        var mock = new Mock<INatsJSContext>();

        mock.Setup(x => x.ListStreamNamesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(streamNames));

        return mock.Object;
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(string[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }

        await Task.CompletedTask;
    }

    private static NatsMessagingTopology TopologyWithConsumer(params string[] subjects)
    {
        var topology = TestTopology.Create();

        topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            FilterSubjects = [.. subjects]
        });

        return topology;
    }

    [Fact]
    public async Task ResolveAsync_Should_BindTheCapturingStream_When_TheServerKnowsIt()
    {
        // arrange
        var topology = TopologyWithConsumer("order-service.order-created");

        // act
        await NatsStreamResolver.ResolveAsync(
            JetStreamReturning("ORDER_SERVICE"),
            topology,
            CancellationToken.None);

        // assert
        Assert.Equal("ORDER_SERVICE", topology.Consumers[0].StreamName);
    }

    [Fact]
    public async Task ResolveAsync_Should_PreferALocalStream_When_OneIsDeclared()
    {
        // arrange
        var topology = TestTopology.Create();

        topology.AddStream(new NatsStreamConfiguration
        {
            Name = "ORDER_SERVICE",
            Subjects = ["order-service.>"]
        });

        topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            FilterSubjects = ["order-service.order-created"]
        });

        var jetStream = new Mock<INatsJSContext>(MockBehavior.Strict);

        // act
        await NatsStreamResolver.ResolveAsync(jetStream.Object, topology, CancellationToken.None);

        // assert
        Assert.Equal("ORDER_SERVICE", topology.Consumers[0].StreamName);
        jetStream.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_Should_LeaveTheStream_When_TheConsumerAlreadyNamesOne()
    {
        // arrange
        var topology = TestTopology.Create();

        topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            StreamName = "LEGACY_ORDERS",
            FilterSubjects = ["order-service.order-created"]
        });

        var jetStream = new Mock<INatsJSContext>(MockBehavior.Strict);

        // act
        await NatsStreamResolver.ResolveAsync(jetStream.Object, topology, CancellationToken.None);

        // assert
        Assert.Equal("LEGACY_ORDERS", topology.Consumers[0].StreamName);
        jetStream.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_NoStreamCapturesTheSubject()
    {
        // arrange
        var topology = TopologyWithConsumer("order-service.order-created");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await NatsStreamResolver.ResolveAsync(
                JetStreamReturning(),
                topology,
                CancellationToken.None));

        // assert
        Assert.Contains("No stream captures subject", exception.Message, StringComparison.Ordinal);
        Assert.Contains("FromStream", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_SeveralStreamsCaptureTheSubject()
    {
        // arrange
        var topology = TopologyWithConsumer("order-service.order-created");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await NatsStreamResolver.ResolveAsync(
                JetStreamReturning("ORDER_SERVICE", "ORDER_ARCHIVE"),
                topology,
                CancellationToken.None));

        // assert
        Assert.Contains("captured by 2 streams", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_TheConsumerHasNoSubjects()
    {
        // arrange
        var topology = TopologyWithConsumer();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await NatsStreamResolver.ResolveAsync(
                JetStreamReturning("ORDER_SERVICE"),
                topology,
                CancellationToken.None));

        // assert
        Assert.Contains("has no subjects", exception.Message, StringComparison.Ordinal);
    }
}
