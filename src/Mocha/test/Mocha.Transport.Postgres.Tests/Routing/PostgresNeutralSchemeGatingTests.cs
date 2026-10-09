using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Routing;

public class PostgresNeutralSchemeGatingTests
{
    [Fact]
    public void NeutralScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // Two Postgres transports; the one under test carries IsDefaultTransport().
        // queue: is a neutral scheme supported by Postgres transports.
        var runtime = CreateRuntime(b => b
            .AddPostgres(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionString(DummyConnectionString);
            })
            .AddPostgres(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionString(DummyConnectionString);
            }));
        var primary = runtime.Transports.OfType<PostgresMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var configuration = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void NeutralScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // Two Postgres transports; the one under test is not the default. It still advertises
        // capability, while EndpointRouter decides whether this candidate is selected.
        var runtime = CreateRuntime(b => b
            .AddPostgres(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionString(DummyConnectionString);
            })
            .AddPostgres(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionString(DummyConnectionString);
            }));
        var secondary = runtime.Transports.OfType<PostgresMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var configuration = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void TopicScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // The default Postgres transport claims the topic: neutral scheme in addition to queue:.
        var runtime = CreateRuntime(b => b
            .AddPostgres(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionString(DummyConnectionString);
            })
            .AddPostgres(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionString(DummyConnectionString);
            }));
        var primary = runtime.Transports.OfType<PostgresMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var configuration = primary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void TopicScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // A non-default Postgres transport still supports topic: URIs.
        var runtime = CreateRuntime(b => b
            .AddPostgres(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionString(DummyConnectionString);
            })
            .AddPostgres(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionString(DummyConnectionString);
            }));
        var secondary = runtime.Transports.OfType<PostgresMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var configuration = secondary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.NotNull(configuration);
    }

    private const string DummyConnectionString =
        "Host=localhost;Database=mocha_test;Username=test;Password=test";

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder.BuildRuntime();
    }
}
