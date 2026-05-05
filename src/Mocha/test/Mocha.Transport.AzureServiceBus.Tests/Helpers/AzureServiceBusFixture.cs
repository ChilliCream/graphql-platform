using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Squadron;
using Xunit.Abstractions;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

public class MochaAzureServiceBusOptions : AzureCloudServiceBusOptions
{
    public override void Configure(ServiceBusOptionsBuilder builder)
    {
        builder.Namespace("squadron-mocha");
    }
}

public class MochaAzureServiceBusResource : AzureCloudServiceBusResource<MochaAzureServiceBusOptions>
{
    public MochaAzureServiceBusResource(IMessageSink messageSink)
        : base(messageSink)
    {
    }
}

public sealed class AzureServiceBusFixture : IAsyncLifetime
{
    private readonly MochaAzureServiceBusResource _resource;

    public AzureServiceBusFixture(IMessageSink messageSink)
    {
        _resource = new MochaAzureServiceBusResource(messageSink);
    }

    public async Task InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public string ConnectionString => _resource.ConnectionString;

    /// <summary>
    /// Creates a test context with a unique name prefix derived from the calling test method.
    /// The prefix is used to create isolated queue/topic names so tests do not interfere with each other.
    /// </summary>
    public TestContext CreateTestContext(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var prefix = GeneratePrefix(testName, filePath);
        return new TestContext(this, prefix);
    }

    private static string GeneratePrefix(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"{testName}_{hash}";
    }
}

/// <summary>
/// Per-test isolation context providing the connection string and a unique name prefix
/// for queues, topics, and subscriptions so tests do not collide.
/// </summary>
public sealed class TestContext(AzureServiceBusFixture fixture, string prefix)
{
    /// <summary>
    /// Gets the connection string to the Azure Service Bus namespace.
    /// </summary>
    public string ConnectionString => fixture.ConnectionString;

    /// <summary>
    /// Gets a unique prefix for this test. Use this to namespace queue and topic names.
    /// </summary>
    public string Prefix => prefix;

    /// <summary>
    /// Returns a unique queue name by combining the prefix with the given base name.
    /// </summary>
    public string QueueName(string baseName) => $"{prefix}-{baseName}";

    /// <summary>
    /// Returns a unique topic name by combining the prefix with the given base name.
    /// </summary>
    public string TopicName(string baseName) => $"{prefix}-{baseName}";

    /// <summary>
    /// Returns a unique subscription name by combining the prefix with the given base name.
    /// </summary>
    public string SubscriptionName(string baseName) => $"{prefix}-{baseName}";
}

[CollectionDefinition("AzureServiceBus")]
public class AzureServiceBusCollection : ICollectionFixture<AzureServiceBusFixture>;
