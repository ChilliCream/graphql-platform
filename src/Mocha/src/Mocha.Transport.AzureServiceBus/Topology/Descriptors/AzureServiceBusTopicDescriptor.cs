namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Descriptor implementation for configuring an Azure Service Bus topic.
/// </summary>
internal sealed class AzureServiceBusTopicDescriptor
    : MessagingDescriptorBase<AzureServiceBusTopicConfiguration>
    , IAzureServiceBusTopicDescriptor
{
    public AzureServiceBusTopicDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new AzureServiceBusTopicConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override AzureServiceBusTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive)
    {
        Configuration.DefaultMessageTimeToLive = defaultMessageTimeToLive;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes)
    {
        Configuration.MaxSizeInMegabytes = maxSizeInMegabytes;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithEnablePartitioning(bool enablePartitioning = true)
    {
        Configuration.EnablePartitioning = enablePartitioning;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithRequiresDuplicateDetection(bool requiresDuplicateDetection = true)
    {
        Configuration.RequiresDuplicateDetection = requiresDuplicateDetection;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithDuplicateDetectionHistoryTimeWindow(TimeSpan duplicateDetectionHistoryTimeWindow)
    {
        Configuration.DuplicateDetectionHistoryTimeWindow = duplicateDetectionHistoryTimeWindow;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle)
    {
        Configuration.AutoDeleteOnIdle = autoDeleteOnIdle;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusTopicDescriptor WithSupportOrdering(bool supportOrdering = true)
    {
        Configuration.SupportOrdering = supportOrdering;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public AzureServiceBusTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The topic name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static AzureServiceBusTopicDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
