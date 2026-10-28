using Moq;
using NATS.Client.JetStream;

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
        Assert.Equal(
            "No stream captures subject 'order-service.order-created', required by consumer "
            + "'order-service_order-created'. The publishing service may not have been deployed yet, "
            + "or its stream was never provisioned. Declare the stream with FromStream so this "
            + "service provisions it regardless of start-up order.",
            exception.Message);
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
        Assert.Equal(
            "Subject 'order-service.order-created' is captured by 2 streams (ORDER_SERVICE, "
            + "ORDER_ARCHIVE), required by consumer 'order-service_order-created'. The publishing "
            + "service may not have been deployed yet, or its stream was never provisioned. Declare "
            + "the stream with FromStream so this service provisions it regardless of start-up order.",
            exception.Message);
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
        Assert.Equal(
            "Consumer 'order-service_order-created' has no subjects, so its stream cannot be "
            + "resolved. Declare the stream explicitly with FromStream.",
            exception.Message);
    }
}
