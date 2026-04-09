using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Testcontainers.Kafka;

namespace Mocha.Transport.Kafka.Tests.Helpers;

public sealed class KafkaFixture : IAsyncLifetime
{
    private KafkaContainer? _container;

    public string BootstrapServers { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new KafkaBuilder("confluentinc/cp-kafka:7.6.0")
            .Build();

        await _container.StartAsync();
        BootstrapServers = _container.GetBootstrapAddress();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public KafkaTestContext CreateTestContext(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var suffix = GenerateSuffix(testName, filePath);
        return new KafkaTestContext(BootstrapServers, suffix);
    }

    private static string GenerateSuffix(string testName, string filePath)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(filePath));
        return $"{testName}_{Convert.ToHexString(hash[..4]).ToLowerInvariant()}";
    }
}

public sealed class KafkaTestContext(string bootstrapServers, string suffix)
{
    public string BootstrapServers => bootstrapServers;

    public string TopicPrefix => suffix;

    public string GetTopicName(string baseName) => $"{baseName}-{suffix}";
}

[CollectionDefinition("Kafka")]
public class KafkaCollection : ICollectionFixture<KafkaFixture>;
