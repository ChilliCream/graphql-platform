using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsStreamTests
{
    private static NatsStream CreateStream(NatsStreamConfiguration configuration)
        => TestTopology.Create().AddStream(configuration);

    [Fact]
    public void Initialize_Should_ReadTheDeclaredSubjects_When_TheyAreGiven()
    {
        // arrange
        var configuration = new NatsStreamConfiguration
        {
            Name = "ORDER_SERVICE",
            Subjects = ["order-service.>"]
        };

        // act
        var stream = CreateStream(configuration);

        // assert
        Assert.Equal("ORDER_SERVICE", stream.Name);
        Assert.Equal(["order-service.>"], stream.Subjects);
    }

    [Fact]
    public void Initialize_Should_LeaveTheWindowToTheServer_When_NoneIsDeclared()
    {
        // arrange
        // Zero is not "deduplication off": the client omits a zero window from the request and the
        // server applies its own default. There is no way to disable it from here.
        var configuration = new NatsStreamConfiguration { Name = "ORDER_SERVICE" };

        // act
        var stream = CreateStream(configuration);

        // assert
        Assert.Equal(TimeSpan.Zero, stream.DuplicateWindow);
    }

    [Fact]
    public void Initialize_Should_Throw_When_TheStreamNameContainsADot()
    {
        // arrange
        var configuration = new NatsStreamConfiguration { Name = "order.service" };

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateStream(configuration));

        // assert
        Assert.Contains("not a valid JetStream stream name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_Should_Throw_When_NoNameIsGiven()
    {
        // act and assert
        Assert.Throws<InvalidOperationException>(() => CreateStream(new NatsStreamConfiguration()));
    }
}

public class NatsConsumerTests
{
    private static NatsConsumer CreateConsumer(NatsConsumerConfiguration configuration)
        => TestTopology.Create().AddConsumer(configuration);

    [Fact]
    public void Initialize_Should_LeaveTheStreamUnresolved_When_NoneIsDeclared()
    {
        // arrange
        var configuration = new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            FilterSubjects = ["order-service.order-created"]
        };

        // act
        var consumer = CreateConsumer(configuration);

        // assert
        Assert.Null(consumer.StreamName);
        Assert.Equal(["order-service.order-created"], consumer.FilterSubjects);
    }

    [Fact]
    public void Initialize_Should_KeepTheStream_When_OneIsDeclared()
    {
        // arrange
        var configuration = new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            StreamName = "ORDER_SERVICE"
        };

        // act
        var consumer = CreateConsumer(configuration);

        // assert
        Assert.Equal("ORDER_SERVICE", consumer.StreamName);
    }

    [Fact]
    public void Initialize_Should_Throw_When_TheDurableNameContainsADot()
    {
        // arrange
        var configuration = new NatsConsumerConfiguration { Name = "order-service.order-created" };

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateConsumer(configuration));

        // assert
        Assert.Contains("not a valid JetStream consumer name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProvisionAsync_Should_Throw_When_TheStreamIsUnresolved()
    {
        // arrange
        var consumer = CreateConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            FilterSubjects = ["order-service.order-created"]
        });

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await consumer.ProvisionAsync(null!, CancellationToken.None));

        // assert
        Assert.Contains("has not been resolved", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Merge_Should_UnionFilterSubjects_When_TheConsumerIsDeclaredTwice()
    {
        // arrange
        // An explicit declaration and an endpoint-derived one can resolve to the same durable name,
        // and the endpoint's subjects must survive the fold.
        var topology = TestTopology.Create();

        topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            AckWait = TimeSpan.FromSeconds(30)
        });

        // act
        var consumer = topology.AddConsumer(new NatsConsumerConfiguration
        {
            Name = "order-service_order-created",
            FilterSubjects = ["order-service.order-created"],
            StreamName = "ORDER_SERVICE"
        });

        // assert
        Assert.Equal(["order-service.order-created"], consumer.FilterSubjects);
        Assert.Equal("ORDER_SERVICE", consumer.StreamName);
    }
}
