using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.Kafka.Tests.Helpers;

public static class KafkaBusFixture
{
    private const string DummyBootstrapServers = "localhost:9092";

    public static (
        MessagingRuntime Runtime,
        KafkaMessagingTransport Transport,
        KafkaMessagingTopology Topology) CreateTopology(
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddKafka(t => t.BootstrapServers(DummyBootstrapServers))
            .BuildRuntime();
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var topology = (KafkaMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    public static (
        MessagingRuntime Runtime,
        KafkaMessagingTransport Transport,
        KafkaMessagingTopology Topology) CreateTopologyWithTransport(
        Action<IKafkaMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddKafka(t =>
            {
                t.BootstrapServers(DummyBootstrapServers);
                configure(t);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var topology = (KafkaMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    public static MessagingRuntime CreateRuntime(Action<IKafkaMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddKafka(t =>
            {
                t.BootstrapServers(DummyBootstrapServers);
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
