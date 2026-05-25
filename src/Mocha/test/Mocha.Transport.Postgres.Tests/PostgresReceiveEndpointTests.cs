using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresReceiveEndpointTests
{
    [Fact]
    public void ReceiveEndpoint_Should_ResolveQueue_When_QueueNameConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.Endpoint("ep").Queue("my-q").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.Equal("my-q", endpoint.Queue.Name);
        Assert.IsType<PostgresQueue>(endpoint.Source);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetFaultEndpoint_When_FaultEndpointConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DeclareQueue("q_error");
            t.Endpoint("ep").Queue("q").Handler<OrderCreatedHandler>().FaultEndpoint("postgres:///q/q_error");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "q");

        // assert
        Assert.NotNull(endpoint.ErrorEndpoint);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSkippedEndpoint_When_SkippedEndpointConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DeclareQueue("q_skipped");
            t.Endpoint("ep").Queue("q").Handler<OrderCreatedHandler>().SkippedEndpoint("postgres:///q/q_skipped");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "q");

        // assert
        Assert.NotNull(endpoint.SkippedEndpoint);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("err-q");
            t.Endpoint("err-ep").Queue("err-q").Kind(ReceiveEndpointKind.Error);
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "err-q");

        // assert
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSourceAddress_When_QueueConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.Endpoint("ep").Queue("my-q").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.NotNull(endpoint.Source.Address);
        Assert.Contains("q/my-q", endpoint.Source.Address.ToString());
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.Endpoint("ep").Queue("my-q").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<PostgresReceiveEndpoint>().First(e => e.Queue.Name == "my-q");

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("postgres", endpoint.Address.Scheme);
    }

    private static MessagingRuntime CreateRuntime(Action<IPostgresMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
