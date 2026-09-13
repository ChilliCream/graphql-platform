using Moq;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsConsumerAckOptionsTests
{
    [Fact]
    public void Initialize_Should_LeaveAckProgressOff_When_NoIntervalIsDeclared()
    {
        // arrange
        // Reporting progress costs a background task per in-flight message, so it is opt-in.
        var configuration = new NatsConsumerConfiguration { Name = "order-service_order-created" };

        // act
        var consumer = TestTopology.Create().AddConsumer(configuration);

        // assert
        Assert.Null(consumer.AckProgressInterval);
    }

    [Fact]
    public void AckProgressEvery_Should_ReachTheConsumer_When_Declared()
    {
        // arrange
        var descriptor = new NatsMessagingTransportDescriptor(Mock.Of<IMessagingSetupContext>());

        descriptor
            .DeclareConsumer("order-service_order-created")
            .AckWait(TimeSpan.FromSeconds(30))
            .AckProgressEvery(TimeSpan.FromSeconds(10));

        // act
        var configuration = Assert.Single(descriptor.CreateConfiguration().Consumers);
        var consumer = TestTopology.Create().AddConsumer(configuration);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(10), consumer.AckProgressInterval);
    }

    [Fact]
    public void Initialize_Should_UseJetStreamsOwnCeiling_When_NoMaxAckPendingIsDeclared()
    {
        // arrange
        // MaxAckPending is the server-side ceiling shared by every instance reading the durable, so
        // it is deliberately not derived from one instance's MaxConcurrency.
        var configuration = new NatsConsumerConfiguration { Name = "order-service_order-created" };

        // act
        var consumer = TestTopology.Create().AddConsumer(configuration);

        // assert
        Assert.Equal(NatsConsumer.DefaultMaxAckPending, consumer.MaxAckPending);
    }
}
