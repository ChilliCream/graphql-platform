namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Descriptor implementation for configuring an Event Hub subscription (consumer group).
/// </summary>
internal sealed class EventHubSubscriptionDescriptor
    : MessagingDescriptorBase<EventHubSubscriptionConfiguration>
    , IEventHubSubscriptionDescriptor
{
    private EventHubSubscriptionDescriptor(
        IMessagingConfigurationContext context,
        string topicName,
        string consumerGroup) : base(context)
    {
        Configuration = new EventHubSubscriptionConfiguration
        {
            TopicName = topicName,
            ConsumerGroup = consumerGroup
        };
    }

    /// <inheritdoc />
    protected internal override EventHubSubscriptionConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IEventHubSubscriptionDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final subscription configuration.
    /// </summary>
    /// <returns>The configured subscription configuration.</returns>
    public EventHubSubscriptionConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new subscription descriptor.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="topicName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <returns>A new subscription descriptor.</returns>
    public static EventHubSubscriptionDescriptor New(
        IMessagingConfigurationContext context,
        string topicName,
        string consumerGroup)
        => new(context, topicName, consumerGroup);
}
