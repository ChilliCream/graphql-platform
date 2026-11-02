using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.Postgres.Tests.Helpers;

public static class PostgresBusFixture
{
    private const string DummyConnectionString = "Host=localhost;Database=mocha_test;Username=test;Password=test";

    public static (
        MessagingRuntime Runtime,
        PostgresMessagingTransport Transport,
        PostgresMessagingTopology Topology) CreateTopology(
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString(DummyConnectionString))
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    public static (
        MessagingRuntime Runtime,
        PostgresMessagingTransport Transport,
        PostgresMessagingTopology Topology) CreateTopologyWithTransport(
        Action<IPostgresMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configure(t);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    public static MessagingRuntime CreateRuntime(Action<IPostgresMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}

internal static class MessageBusHostBuilderTestExtensions
{
    public static MessagingRuntime BuildRuntime(this IMessageBusHostBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }
}
