using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

public class PostgresDescriptorTests
{
    [Fact]
    public void Transport_Should_UseCustomSchema_When_SchemaConfigured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.Schema("custom"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal("custom", transport.Schema);
    }

    [Fact]
    public void Transport_Should_UseCustomName_When_NameConfigured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.Name("my-transport"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal("my-transport", transport.Name);
    }

    [Fact]
    public void Transport_Should_BeDefault_When_IsDefaultTransportCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.IsDefaultTransport());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.NotNull(transport);
        Assert.Single(runtime.Transports);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetQueue_When_ToQueueCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresQueue>(endpoint.Destination);
        Assert.Equal("my-q", ((PostgresQueue)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Queue);
        Assert.Equal("my-q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_ToTopicCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-t");
            t.DispatchEndpoint("ep").ToTopic("my-t");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresTopic>(endpoint.Destination);
        Assert.Equal("my-t", ((PostgresTopic)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("my-t", endpoint.Topic!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterSendRoute_When_SendCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DispatchEndpoint("ep").ToQueue("q").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresQueue>(endpoint.Destination);
        Assert.Equal("q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterPublishRoute_When_PublishCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("t");
            t.DispatchEndpoint("ep").ToTopic("t").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresTopic>(endpoint.Destination);
        Assert.Equal("t", endpoint.Topic!.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetQueueName_When_QueueCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("custom-q");
            t.Endpoint("ep").Queue("custom-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.Equal("custom-q", endpoint.Queue.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("err-q");
            t.Endpoint("ep").Queue("err-q").Kind(ReceiveEndpointKind.Error);
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetFaultEndpoint_When_FaultEndpointCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DeclareQueue("q_err");
            t.Endpoint("ep").Queue("q").FaultEndpoint("postgres:///q/q_err");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.NotNull(endpoint.ErrorEndpoint);
        Assert.Contains("q_err", endpoint.ErrorEndpoint!.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSkippedEndpoint_When_SkippedEndpointCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DeclareQueue("q_skip");
            t.Endpoint("ep").Queue("q").SkippedEndpoint("postgres:///q/q_skip");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.NotNull(endpoint.SkippedEndpoint);
        Assert.Contains("q_skip", endpoint.SkippedEndpoint!.Name);
    }
}
