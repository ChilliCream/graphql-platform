using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CookieCrumble.Resources;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

public sealed class KafkaFixture : IAsyncLifetime
{
    private readonly KafkaResource _resource = new();

    public string BootstrapServers => _resource.BootstrapServers;

    public async ValueTask InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public async Task CreateTopicAsync(string topic, CancellationToken cancellationToken, int partitions = 1)
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
                        NumPartitions = partitions,
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

    public async Task IncreasePartitionsAsync(
        string topic,
        int newTotalCount,
        CancellationToken cancellationToken)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = BootstrapServers })
            .Build();

        await admin.CreatePartitionsAsync(
            [
                new PartitionsSpecification
                {
                    Topic = topic,
                    IncreaseTo = newTotalCount
                }
            ],
            new CreatePartitionsOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(20)
            });

        await WaitForPartitionCountMetadataAsync(admin, topic, newTotalCount, cancellationToken);
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

    private static async Task WaitForPartitionCountMetadataAsync(
        IAdminClient admin,
        string topic,
        int partitionCount,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5));

            if (metadata.Topics.Any(
                t => t.Topic == topic
                    && t.Error.Code == ErrorCode.NoError
                    && t.Partitions.Count >= partitionCount))
            {
                return;
            }

            await Task.Delay(100, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
