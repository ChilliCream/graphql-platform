using Azure.Identity;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal sealed class AzureEventHubsEventStreamBrokerProvider : IEventStreamBrokerProvider
{
    private readonly AzureEventHubsEventStreamOptions _options;

    public AzureEventHubsEventStreamBrokerProvider(
        string name,
        IOptionsMonitor<AzureEventHubsEventStreamOptions> options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Get(name);
        Validate(_options);
    }

    public IEventStreamBroker Create()
        => new AzureEventHubsEventStreamBroker(_options);

    private static void Validate(AzureEventHubsEventStreamOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConsumerGroup))
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a consumer group.");
        }

        if (options.MaximumWaitTime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a positive maximum wait time.");
        }

        if (options.SeedingQueryTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a positive seeding query timeout.");
        }

        if (options.SeedingDeadline <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a positive seeding deadline.");
        }

        if (options.SeedingDeadline < options.SeedingQueryTimeout)
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a seeding deadline that is at least the seeding query timeout.");
        }

        if (options.PartitionDiscoveryInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Azure Event Hubs event stream broker options require a positive partition discovery interval.");
        }

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            options.Credential ??= new DefaultAzureCredential();
            return;
        }

        throw new InvalidOperationException(
            "Azure Event Hubs event stream broker options require a connection string or namespace.");
    }
}
