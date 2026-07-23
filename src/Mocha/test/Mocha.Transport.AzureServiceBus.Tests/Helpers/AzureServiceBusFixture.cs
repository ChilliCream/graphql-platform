using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Testcontainers.ServiceBus;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

public sealed class AzureServiceBusFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _container;
    private ServiceBusAdministrationClient? _adminClient;

    public AzureServiceBusFixture()
    {
        _container = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1")
            .WithAcceptLicenseAgreement(true)
            .Build();
    }

    /// <summary>
    /// Gets whether scheduled message cancellation is supported. Emulator bug #119 means
    /// CancelScheduledMessageAsync does not cancel delivery.
    /// </summary>
    public static bool SupportsScheduledCancellation => false;

    /// <summary>
    /// Gets whether runtime message counts are supported. Runtime properties always report 0 counts.
    /// </summary>
    public static bool SupportsRuntimeMessageCounts => false;

    /// <summary>
    /// Gets whether partitioning is supported. EnablePartitioning is silently ignored.
    /// </summary>
    public static bool SupportsPartitioning => false;

    /// <summary>
    /// Gets whether AMQP WebSockets are supported. The emulator supports AMQP TCP only.
    /// </summary>
    public static bool SupportsWebSockets => false;

    /// <summary>
    /// Gets whether Entra authentication is supported. The emulator supports SAS connection strings only,
    /// with no TokenCredential support.
    /// </summary>
    public static bool SupportsEntraAuth => false;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _adminClient = new ServiceBusAdministrationClient(_container.GetHttpConnectionString());
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    public string ConnectionString => _container.GetConnectionString();

    public string AdminConnectionString => _container.GetHttpConnectionString();

    /// <summary>
    /// Creates a test context with a unique name prefix derived from the calling test method.
    /// The prefix isolates explicitly named entities while tests are running. Disposing the context
    /// removes all non-default entities from the namespace to remain within the emulator entity limit.
    /// </summary>
    public TestContext CreateTestContext(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var prefix = GeneratePrefix(testName, filePath);
        return new TestContext(this, prefix);
    }

    internal async ValueTask CleanupEntitiesAsync()
    {
        var adminClient = _adminClient
            ?? throw new InvalidOperationException("The Azure Service Bus fixture has not been initialized.");

        await foreach (var queue in adminClient.GetQueuesAsync())
        {
            if (!string.Equals(queue.Name, "queue.1", StringComparison.Ordinal))
            {
                try
                {
                    await adminClient.DeleteQueueAsync(queue.Name);
                }
                catch (ServiceBusException ex)
                    when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                }
            }
        }

        await foreach (var topic in adminClient.GetTopicsAsync())
        {
            if (!string.Equals(topic.Name, "topic.1", StringComparison.Ordinal))
            {
                try
                {
                    await adminClient.DeleteTopicAsync(topic.Name);
                }
                catch (ServiceBusException ex)
                    when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                }
            }
        }
    }

    private static string GeneratePrefix(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"{testName}_{hash}";
    }
}

/// <summary>
/// Per-test isolation context providing connection strings and a unique name prefix for queues,
/// topics, and subscriptions. Disposing the context removes all non-default entities from the namespace.
/// </summary>
public sealed class TestContext(AzureServiceBusFixture fixture, string prefix) : IAsyncDisposable
{
    /// <summary>
    /// Gets the connection string to the Azure Service Bus namespace.
    /// </summary>
    public string ConnectionString => fixture.ConnectionString;

    /// <summary>
    /// Gets the connection string for Azure Service Bus management operations.
    /// </summary>
    public string AdminConnectionString => fixture.AdminConnectionString;

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

    public ValueTask DisposeAsync() => fixture.CleanupEntitiesAsync();
}

[CollectionDefinition("AzureServiceBus")]
public class AzureServiceBusCollection : ICollectionFixture<AzureServiceBusFixture>;
