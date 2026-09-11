using CookieCrumble;
using Moq;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsMessagingTransportDescriptorTests
{
    private static NatsMessagingTransportDescriptor CreateDescriptor()
        => new(Mock.Of<IMessagingSetupContext>());

    [Fact]
    public void DeclareStream_Should_CollectEverySetting_When_Configured()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor
            .DeclareStream("ORDER_SERVICE")
            .Subject("order-service.>")
            .Retention(StreamConfigRetention.Interest)
            .MaxAge(TimeSpan.FromDays(7))
            .MaxMessages(10_000)
            .MaxBytes(1_048_576)
            .Replicas(3)
            .DeduplicateWithin(TimeSpan.FromMinutes(2));

        // assert
        Describe(Assert.Single(descriptor.CreateConfiguration().Streams)).MatchInlineSnapshot(
            """
            name: ORDER_SERVICE
            subjects: order-service.>
            retention: Interest
            maxAge: 7.00:00:00
            maxMsgs: 10000
            maxBytes: 1048576
            replicas: 3
            duplicateWindow: 00:02:00
            """);
    }

    [Fact]
    public void DeclareStream_Should_ReturnTheSameDeclaration_When_TheNameRepeats()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.DeclareStream("ORDER_SERVICE").Subject("order-service.>");
        descriptor.DeclareStream("ORDER_SERVICE").Subject("order-service-legacy.>");

        // assert
        var stream = Assert.Single(descriptor.CreateConfiguration().Streams);

        Assert.Equal(["order-service.>", "order-service-legacy.>"], stream.Subjects);
    }

    [Fact]
    public void DeclareConsumer_Should_CollectEverySetting_When_Configured()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor
            .DeclareConsumer("order-service_order-created")
            .Subject("order-service.order-created")
            .FromStream("ORDER_SERVICE")
            .AckWait(TimeSpan.FromSeconds(30))
            .MaxAckPending(500)
            .MaxDeliver(10)
            .DeliverFrom(ConsumerConfigDeliverPolicy.New)
            .Backoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        // assert
        Describe(Assert.Single(descriptor.CreateConfiguration().Consumers)).MatchInlineSnapshot(
            """
            name: order-service_order-created
            stream: ORDER_SERVICE
            filterSubjects: order-service.order-created
            ackWait: 00:00:30
            maxAckPending: 500
            maxDeliver: 10
            deliverPolicy: New
            backoff: 00:00:01, 00:00:05
            """);
    }

    [Fact]
    public void StreamName_Should_ReachTheConfiguration_When_Set()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.StreamName("order-service").AutoProvision(false);

        // assert
        var configuration = descriptor.CreateConfiguration();

        Assert.Equal("order-service", configuration.StreamName);
        Assert.False(configuration.AutoProvision);
    }

    [Fact]
    public void AddDefaults_Should_SetTheNatsSchema_When_Applied()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.AddDefaults();

        // assert
        Assert.Equal(
            NatsTransportConfiguration.DefaultSchema,
            descriptor.CreateConfiguration().Schema);
    }

    [Fact]
    public void Subject_Should_Deduplicate_When_TheSameSubjectIsDeclaredTwice()
    {
        // arrange
        var descriptor = CreateDescriptor();

        // act
        descriptor.DeclareStream("ORDER_SERVICE").Subject("order-service.>").Subject("order-service.>");

        // assert
        Assert.Equal(
            ["order-service.>"],
            Assert.Single(descriptor.CreateConfiguration().Streams).Subjects ?? []);
    }

    private static string Describe(NatsStreamConfiguration stream)
        => string.Join(
            "\n",
            $"name: {stream.Name}",
            $"subjects: {string.Join(", ", stream.Subjects ?? [])}",
            $"retention: {stream.Retention}",
            $"maxAge: {stream.MaxAge}",
            $"maxMsgs: {stream.MaxMsgs}",
            $"maxBytes: {stream.MaxBytes}",
            $"replicas: {stream.NumReplicas}",
            $"duplicateWindow: {stream.DuplicateWindow}");

    private static string Describe(NatsConsumerConfiguration consumer)
        => string.Join(
            "\n",
            $"name: {consumer.Name}",
            $"stream: {consumer.StreamName}",
            $"filterSubjects: {string.Join(", ", consumer.FilterSubjects ?? [])}",
            $"ackWait: {consumer.AckWait}",
            $"maxAckPending: {consumer.MaxAckPending}",
            $"maxDeliver: {consumer.MaxDeliver}",
            $"deliverPolicy: {consumer.DeliverPolicy}",
            $"backoff: {string.Join(", ", consumer.Backoff ?? [])}");
}
