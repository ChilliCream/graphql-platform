namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Extension methods for routing outbound messages to Azure Service Bus queues or topics.
/// </summary>
public static class AzureServiceBusMessageTypeDescriptorExtensions
{
    /// <summary>Routes the outbound message to an Azure Service Bus queue.</summary>
    public static IOutboundRouteDescriptor ToAzureServiceBusQueue(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string queueName)
        => descriptor.Destination(new Uri($"{schema}:q/{queueName}"));

    /// <summary>Routes the outbound message to an Azure Service Bus queue using the default schema.</summary>
    public static IOutboundRouteDescriptor ToAzureServiceBusQueue(
        this IOutboundRouteDescriptor descriptor,
        string queueName)
        => descriptor.ToAzureServiceBusQueue(AzureServiceBusTransportConfiguration.DefaultSchema, queueName);

    /// <summary>Routes the outbound message to an Azure Service Bus topic.</summary>
    public static IOutboundRouteDescriptor ToAzureServiceBusTopic(
        this IOutboundRouteDescriptor descriptor,
        string schema,
        string topicName)
        => descriptor.Destination(new Uri($"{schema}:t/{topicName}"));

    /// <summary>Routes the outbound message to an Azure Service Bus topic using the default schema.</summary>
    public static IOutboundRouteDescriptor ToAzureServiceBusTopic(
        this IOutboundRouteDescriptor descriptor,
        string topicName)
        => descriptor.ToAzureServiceBusTopic(AzureServiceBusTransportConfiguration.DefaultSchema, topicName);
}
