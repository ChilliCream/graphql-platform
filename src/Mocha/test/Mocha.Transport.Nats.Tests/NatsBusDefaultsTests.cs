using CookieCrumble;
using Moq;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsBusDefaultsTests
{
    [Fact]
    public void ConfigureDefaults_Should_ReachTheConfiguration_When_SetOnTheDescriptor()
    {
        // arrange
        var descriptor = new NatsMessagingTransportDescriptor(Mock.Of<IMessagingSetupContext>());

        // act
        descriptor.ConfigureDefaults(defaults =>
        {
            defaults.Stream.NumReplicas = 3;
            defaults.Consumer.MaxAckPending = 25;
        });

        // assert
        var configuration = descriptor.CreateConfiguration();

        Assert.Equal(
            (3, 25L),
            (configuration.Defaults.Stream.NumReplicas, configuration.Defaults.Consumer.MaxAckPending));
    }

    [Fact]
    public void ApplyTo_Should_FillEverySetting_When_TheStreamLeavesThemUnset()
    {
        // arrange
        var defaults = new NatsDefaultStreamOptions
        {
            Retention = StreamConfigRetention.Interest,
            Storage = StreamConfigStorage.Memory,
            MaxAge = TimeSpan.FromDays(7),
            MaxMsgs = 10_000,
            MaxBytes = 1_048_576,
            NumReplicas = 3,
            DuplicateWindow = TimeSpan.FromMinutes(2)
        };

        var configuration = new NatsStreamConfiguration { Name = "ORDER_SERVICE" };

        // act
        defaults.ApplyTo(configuration);

        // assert
        Describe(configuration).MatchInlineSnapshot(
            """
            retention: Interest
            storage: Memory
            maxAge: 7.00:00:00
            maxMsgs: 10000
            maxBytes: 1048576
            replicas: 3
            duplicateWindow: 00:02:00
            """);
    }

    [Fact]
    public void ApplyTo_Should_KeepEverySetting_When_TheStreamAlreadySetThem()
    {
        // arrange
        var defaults = new NatsDefaultStreamOptions
        {
            Retention = StreamConfigRetention.Interest,
            Storage = StreamConfigStorage.Memory,
            MaxAge = TimeSpan.FromDays(7),
            MaxMsgs = 10_000,
            MaxBytes = 1_048_576,
            NumReplicas = 3,
            DuplicateWindow = TimeSpan.FromMinutes(2)
        };

        var configuration = new NatsStreamConfiguration
        {
            Name = "ORDER_SERVICE",
            Retention = StreamConfigRetention.Limits,
            Storage = StreamConfigStorage.File,
            MaxAge = TimeSpan.FromDays(1),
            MaxMsgs = 1,
            MaxBytes = 2,
            NumReplicas = 1,
            DuplicateWindow = TimeSpan.FromSeconds(30)
        };

        // act
        defaults.ApplyTo(configuration);

        // assert
        Describe(configuration).MatchInlineSnapshot(
            """
            retention: Limits
            storage: File
            maxAge: 1.00:00:00
            maxMsgs: 1
            maxBytes: 2
            replicas: 1
            duplicateWindow: 00:00:30
            """);
    }

    [Fact]
    public void ApplyTo_Should_FillEverySetting_When_TheConsumerLeavesThemUnset()
    {
        // arrange
        var defaults = new NatsDefaultConsumerOptions
        {
            AckWait = TimeSpan.FromSeconds(45),
            MaxDeliver = 5,
            MaxAckPending = 25,
            Backoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)],
            AckProgressInterval = TimeSpan.FromSeconds(10),
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        var configuration = new NatsConsumerConfiguration { Name = "order-service_order-created" };

        // act
        defaults.ApplyTo(configuration);

        // assert
        Describe(configuration).MatchInlineSnapshot(
            """
            ackWait: 00:00:45
            maxDeliver: 5
            maxAckPending: 25
            backoff: 00:00:01, 00:00:05
            ackProgressInterval: 00:00:10
            deliverPolicy: All
            """);
    }

    [Fact]
    public void ApplyTo_Should_KeepEverySetting_When_TheConsumerAlreadySetThem()
    {
        // arrange
        var defaults = new NatsDefaultConsumerOptions
        {
            AckWait = TimeSpan.FromSeconds(45),
            MaxDeliver = 5,
            MaxAckPending = 25,
            Backoff = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)],
            AckProgressInterval = TimeSpan.FromSeconds(10),
            DeliverPolicy = ConsumerConfigDeliverPolicy.All
        };

        var configuration = new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            AckWait = TimeSpan.FromSeconds(30),
            MaxDeliver = 2,
            MaxAckPending = 1,
            Backoff = [TimeSpan.FromSeconds(9)],
            AckProgressInterval = TimeSpan.FromSeconds(3),
            DeliverPolicy = ConsumerConfigDeliverPolicy.New
        };

        // act
        defaults.ApplyTo(configuration);

        // assert
        Describe(configuration).MatchInlineSnapshot(
            """
            ackWait: 00:00:30
            maxDeliver: 2
            maxAckPending: 1
            backoff: 00:00:09
            ackProgressInterval: 00:00:03
            deliverPolicy: New
            """);
    }

    [Fact]
    public void AddStream_Should_ApplyBusDefaults_When_TheStreamLeavesThemUnset()
    {
        // arrange
        var defaults = new NatsBusDefaults
        {
            Stream = new NatsDefaultStreamOptions { DuplicateWindow = TimeSpan.FromMinutes(2) }
        };

        var topology = TestTopology.Create(defaults);

        // act
        var stream = topology.AddStream(new NatsStreamConfiguration { Name = "ORDER_SERVICE" });

        // assert
        Assert.Equal(TimeSpan.FromMinutes(2), stream.DuplicateWindow);
    }

    [Fact]
    public void AddConsumer_Should_ApplyBusDefaults_When_TheConsumerLeavesThemUnset()
    {
        // arrange
        var defaults = new NatsBusDefaults
        {
            Consumer = new NatsDefaultConsumerOptions
            {
                MaxAckPending = 25,
                AckProgressInterval = TimeSpan.FromSeconds(10)
            }
        };

        var topology = TestTopology.Create(defaults);

        // act
        var consumer = topology.AddConsumer(
            new NatsConsumerConfiguration { Name = "order-service_order-created" });

        // assert
        Assert.Equal(
            (25L, TimeSpan.FromSeconds(10)),
            (consumer.MaxAckPending, consumer.AckProgressInterval));
    }

    [Fact]
    public void AddConsumer_Should_KeepItsOwnSettings_When_BusDefaultsAlsoSetThem()
    {
        // arrange
        var defaults = new NatsBusDefaults
        {
            Consumer = new NatsDefaultConsumerOptions { MaxAckPending = 25 }
        };

        var topology = TestTopology.Create(defaults);

        // act
        var consumer = topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            MaxAckPending = 1
        });

        // assert
        Assert.Equal(1, consumer.MaxAckPending);
    }

    private static string Describe(NatsStreamConfiguration stream)
        => string.Join(
            "\n",
            $"retention: {stream.Retention}",
            $"storage: {stream.Storage}",
            $"maxAge: {stream.MaxAge}",
            $"maxMsgs: {stream.MaxMsgs}",
            $"maxBytes: {stream.MaxBytes}",
            $"replicas: {stream.NumReplicas}",
            $"duplicateWindow: {stream.DuplicateWindow}");

    private static string Describe(NatsConsumerConfiguration consumer)
        => string.Join(
            "\n",
            $"ackWait: {consumer.AckWait}",
            $"maxDeliver: {consumer.MaxDeliver}",
            $"maxAckPending: {consumer.MaxAckPending}",
            $"backoff: {string.Join(", ", consumer.Backoff ?? [])}",
            $"ackProgressInterval: {consumer.AckProgressInterval}",
            $"deliverPolicy: {consumer.DeliverPolicy}");
}
