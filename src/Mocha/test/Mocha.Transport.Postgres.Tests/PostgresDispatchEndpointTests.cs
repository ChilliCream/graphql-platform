using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresDispatchEndpointTests
{
    [Fact]
    public void DispatchEndpoint_Should_TargetQueue_When_QueueConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Queue);
        Assert.Equal("my-q", endpoint.Queue!.Name);
        Assert.Null(endpoint.Topic);
        Assert.IsType<PostgresQueue>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_TopicConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-t");
            t.DispatchEndpoint("ep").ToTopic("my-t");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-t" });

        // assert
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("my-t", endpoint.Topic!.Name);
        Assert.Null(endpoint.Queue);
        Assert.IsType<PostgresTopic>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveCorrectName_When_QueueConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.Equal("ep", endpoint.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveCorrectName_When_TopicConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-t");
            t.DispatchEndpoint("ep").ToTopic("my-t");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-t" });

        // assert
        Assert.Equal("ep", endpoint.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_QueueConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("q/my-q", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_TopicConfigured()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-t");
            t.DispatchEndpoint("ep").ToTopic("my-t");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-t" });

        // assert
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("t/my-t", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<PostgresDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("postgres", endpoint.Address.Scheme);
    }
}
