using System.Net;
using System.Net.Sockets;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaFixture : IAsyncLifetime
{
    private const int KafkaPort = 9092;

    private readonly IContainer _container;
    private readonly int _hostPort;

    public KafkaFixture()
    {
        _hostPort = GetFreeTcpPort();
        _container = new ContainerBuilder("confluentinc/cp-kafka:7.7.0")
            .WithPortBinding(_hostPort, KafkaPort)
            .WithEnvironment("KAFKA_NODE_ID", "1")
            .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
            .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@localhost:9093")
            .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
            .WithEnvironment(
                "KAFKA_ADVERTISED_LISTENERS",
                $"PLAINTEXT://localhost:{_hostPort}")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
            .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
            .WithEnvironment("CLUSTER_ID", "MkU3OEVBNTcwNTJENDM2Qk")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(KafkaPort))
            .Build();
    }

    public string BootstrapServers => $"localhost:{_hostPort}";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task CreateTopicAsync(string topic, CancellationToken cancellationToken)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = BootstrapServers })
            .Build();

        try
        {
            await admin.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                ],
                new CreateTopicsOptions
                {
                    RequestTimeout = TimeSpan.FromSeconds(20)
                });
        }
        catch (CreateTopicsException ex)
            when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }

        await WaitForTopicMetadataAsync(admin, topic, cancellationToken);
    }

    private static async Task WaitForTopicMetadataAsync(
        IAdminClient admin,
        string topic,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5));

            if (metadata.Topics.Any(t => t.Topic == topic && t.Error.Code == ErrorCode.NoError))
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
