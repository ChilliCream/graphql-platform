namespace Mocha.Transport.Kafka;

/// <summary>
/// Descriptor implementation for configuring a Kafka topic.
/// </summary>
internal sealed class KafkaTopicDescriptor
    : MessagingDescriptorBase<KafkaTopicConfiguration>
    , IKafkaTopicDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaTopicDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial topic name.</param>
    public KafkaTopicDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new KafkaTopicConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override KafkaTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IKafkaTopicDescriptor Partitions(int partitions)
    {
        Configuration.Partitions = partitions;
        return this;
    }

    /// <inheritdoc />
    public IKafkaTopicDescriptor ReplicationFactor(short replicationFactor)
    {
        Configuration.ReplicationFactor = replicationFactor;
        return this;
    }

    /// <inheritdoc />
    public IKafkaTopicDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IKafkaTopicDescriptor WithConfig(string key, string value)
    {
        Configuration.TopicConfigs ??= new Dictionary<string, string>();
        Configuration.TopicConfigs[key] = value;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public KafkaTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The topic name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static KafkaTopicDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
