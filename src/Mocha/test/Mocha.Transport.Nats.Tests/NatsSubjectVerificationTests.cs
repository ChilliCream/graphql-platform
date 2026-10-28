using Moq;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Tests;

public class NatsSubjectVerificationTests
{
    [Fact]
    public async Task VerifySubjectsAsync_Should_BindTheSubject_When_ALocalStreamCapturesIt()
    {
        // arrange
        var topology = TestTopology.Create();

        topology.AddStream(new NatsStreamConfiguration
        {
            Name = "ORDER_SERVICE",
            Subjects = ["order-service.>"]
        });

        topology.AddSubject(new NatsSubjectConfiguration
        {
            Subject = "order-service.order-created_error"
        });

        var jetStream = new Mock<INatsJSContext>(MockBehavior.Strict);

        // act
        await NatsStreamResolver.VerifySubjectsAsync(jetStream.Object, topology, CancellationToken.None);

        // assert
        Assert.Equal("ORDER_SERVICE", topology.Subjects[0].StreamName);
        jetStream.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifySubjectsAsync_Should_Throw_When_NoStreamCapturesAnErrorSubject()
    {
        // arrange
        var topology = TestTopology.Create();

        topology.AddStream(new NatsStreamConfiguration
        {
            Name = "ORDER_SERVICE",
            Subjects = ["order-service.>"]
        });

        topology.AddSubject(new NatsSubjectConfiguration { Subject = "order-created_error" });

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await NatsStreamResolver.VerifySubjectsAsync(
                JetStreamReturning(),
                topology,
                CancellationToken.None));

        // assert
        Assert.Equal(
            "No stream captures subject 'order-created_error', which this service publishes to. Add "
            + "it to a stream's subjects, or declare the stream that should capture it. Publishing "
            + "to an uncaptured subject does not fail immediately, it times out waiting for an "
            + "acknowledgement.",
            exception.Message);
    }

    [Fact]
    public async Task VerifySubjectsAsync_Should_BindTheSubject_When_ARemoteStreamCapturesIt()
    {
        // arrange
        var topology = TestTopology.Create();

        topology.AddSubject(new NatsSubjectConfiguration { Subject = "billing-service.invoice-raised" });

        // act
        await NatsStreamResolver.VerifySubjectsAsync(
            JetStreamReturning("BILLING_SERVICE"),
            topology,
            CancellationToken.None);

        // assert
        Assert.Equal("BILLING_SERVICE", topology.Subjects[0].StreamName);
    }

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
}
