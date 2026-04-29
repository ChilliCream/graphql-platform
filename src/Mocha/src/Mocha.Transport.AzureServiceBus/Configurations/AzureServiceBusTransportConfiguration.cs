using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configuration for an Azure Service Bus messaging transport, extending the base transport configuration
/// with Azure Service Bus-specific connection and topology settings.
/// </summary>
public class AzureServiceBusTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is specified.
    /// </summary>
    public const string DefaultName = "azuresb";

    /// <summary>
    /// The default URI schema used for Azure Service Bus transport addresses.
    /// </summary>
    public const string DefaultSchema = "azuresb";

    /// <summary>
    /// Creates a new configuration instance with the default name and schema.
    /// </summary>
    public AzureServiceBusTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets the Azure Service Bus connection string.
    /// Mutually exclusive with <see cref="FullyQualifiedNamespace"/> + <see cref="Credential"/>.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Service Bus namespace (e.g., "mynamespace.servicebus.windows.net").
    /// Used together with <see cref="Credential"/> for token-based authentication.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the token credential for authentication.
    /// Used together with <see cref="FullyQualifiedNamespace"/> for managed identity or AAD-based auth.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the AMQP transport type for the Service Bus client.
    /// Defaults to <see cref="ServiceBusTransportType.AmqpTcp"/>.
    /// </summary>
    public ServiceBusTransportType TransportType { get; set; } = ServiceBusTransportType.AmqpTcp;

    /// <summary>
    /// Gets or sets the retry options for the Service Bus client.
    /// When <c>null</c>, sensible defaults are applied.
    /// </summary>
    public ServiceBusRetryOptions? RetryOptions { get; set; }

    /// <summary>
    /// Gets or sets the declared topics.
    /// </summary>
    public List<AzureServiceBusTopicConfiguration> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the declared queues.
    /// </summary>
    public List<AzureServiceBusQueueConfiguration> Queues { get; set; } = [];

    /// <summary>
    /// Gets or sets the declared subscriptions.
    /// </summary>
    public List<AzureServiceBusSubscriptionConfiguration> Subscriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether topology resources (queues, topics, subscriptions)
    /// should be automatically provisioned on the broker. When <c>null</c>, defaults to <c>true</c>.
    /// Individual resources can override this setting.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to all auto-provisioned queues, topics, and endpoints.
    /// </summary>
    public AzureServiceBusBusDefaults Defaults { get; set; } = new();
}
