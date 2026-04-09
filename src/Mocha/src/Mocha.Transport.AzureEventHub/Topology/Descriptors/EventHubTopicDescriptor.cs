namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Descriptor implementation for configuring an Event Hub topic.
/// </summary>
internal sealed class EventHubTopicDescriptor
    : MessagingDescriptorBase<EventHubTopicConfiguration>
    , IEventHubTopicDescriptor
{
    private EventHubTopicDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new EventHubTopicConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override EventHubTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IEventHubTopicDescriptor PartitionCount(int partitionCount)
    {
        Configuration.PartitionCount = partitionCount;
        return this;
    }

    /// <inheritdoc />
    public IEventHubTopicDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public EventHubTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The Event Hub entity name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static EventHubTopicDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
