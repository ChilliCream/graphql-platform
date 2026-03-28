using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Topology;

public class NatsMessagingTopologyTests
{
    [Fact]
    public void AddStream_Should_CreateStream_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new NatsStreamConfiguration { Name = "test-stream", Subjects = ["test.>"] };

        // act
        var stream = topology.AddStream(config);

        // assert
        Assert.Equal("test-stream", stream.Name);
        Assert.Contains(stream, topology.Streams);
    }

    [Fact]
    public void AddStream_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddStream(new NatsStreamConfiguration { Name = "duplicate-stream", Subjects = ["dup.>"] });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddStream(new NatsStreamConfiguration { Name = "duplicate-stream", Subjects = ["dup2.>"] })
            );
        Assert.Contains("duplicate-stream", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void AddSubject_Should_CreateSubject_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new NatsSubjectConfiguration { Name = "test-subject", StreamName = "test-stream" };

        // act
        var subject = topology.AddSubject(config);

        // assert
        Assert.Equal("test-subject", subject.Name);
        Assert.Contains(subject, topology.Subjects);
    }

    [Fact]
    public void AddSubject_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddSubject(new NatsSubjectConfiguration { Name = "duplicate-subject" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddSubject(new NatsSubjectConfiguration { Name = "duplicate-subject" })
            );
        Assert.Contains("duplicate-subject", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void AddConsumer_Should_CreateConsumer_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new NatsConsumerConfiguration { Name = "test-consumer", StreamName = "test-stream" };

        // act
        var consumer = topology.AddConsumer(config);

        // assert
        Assert.Equal("test-consumer", consumer.Name);
        Assert.Contains(consumer, topology.Consumers);
    }

    [Fact]
    public void AddConsumer_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddConsumer(new NatsConsumerConfiguration { Name = "duplicate-consumer" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddConsumer(new NatsConsumerConfiguration { Name = "duplicate-consumer" })
            );
        Assert.Contains("duplicate-consumer", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void GetStreamForSubject_Should_ReturnStream_When_ExactSubjectMatch()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddStream(new NatsStreamConfiguration { Name = "orders", Subjects = ["orders.created"] });

        // act
        var stream = topology.GetStreamForSubject("orders.created");

        // assert
        Assert.NotNull(stream);
        Assert.Equal("orders", stream.Name);
    }

    [Fact]
    public void GetStreamForSubject_Should_ReturnStream_When_WildcardMatch()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddStream(new NatsStreamConfiguration { Name = "orders", Subjects = ["orders.>"] });

        // act
        var stream = topology.GetStreamForSubject("orders.created");

        // assert
        Assert.NotNull(stream);
        Assert.Equal("orders", stream.Name);
    }

    [Fact]
    public void GetStreamForSubject_Should_ReturnNull_When_NoMatch()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddStream(new NatsStreamConfiguration { Name = "orders", Subjects = ["orders.>"] });

        // act
        var stream = topology.GetStreamForSubject("payments.created");

        // assert
        Assert.Null(stream);
    }

    [Fact]
    public async Task AddStreamAndConsumer_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        var initialStreamCount = topology.Streams.Count;
        var initialConsumerCount = topology.Consumers.Count;

        const int operationCount = 100;

        // act
        var allTasks = Enumerable
            .Range(0, operationCount)
            .SelectMany(i =>
                new Task[]
                {
                    Task.Run(() => topology.AddStream(
                        new NatsStreamConfiguration { Name = $"stream-{i}", Subjects = [$"s{i}.>"] })),
                    Task.Run(() => topology.AddConsumer(
                        new NatsConsumerConfiguration { Name = $"consumer-{i}", StreamName = $"stream-{i}" }))
                }
            )
            .ToList();

        await Task.WhenAll(allTasks);

        // assert
        Assert.Equal(initialStreamCount + operationCount, topology.Streams.Count);
        Assert.Equal(initialConsumerCount + operationCount, topology.Consumers.Count);

        var streamNames = topology.Streams.Select(s => s.Name).ToList();
        Assert.Equal(streamNames.Count, streamNames.Distinct().Count());

        var consumerNames = topology.Consumers.Select(c => c.Name).ToList();
        Assert.Equal(consumerNames.Count, consumerNames.Distinct().Count());

        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Streams, s => s.Name == $"stream-{i}");
            Assert.Contains(topology.Consumers, c => c.Name == $"consumer-{i}");
        }
    }

    private static (
        MessagingRuntime Runtime,
        NatsMessagingTransport Transport,
        NatsMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new NatsConnection(new NatsOpts { Url = "nats://localhost:4222" }));
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.AddNats().BuildRuntime();
        var transport = runtime.Transports.OfType<NatsMessagingTransport>().Single();
        var topology = (NatsMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}
