namespace Mocha.Transport.InMemory;

/// <summary>
/// Descriptor implementation for configuring a InMemory binding.
/// </summary>
internal sealed class InMemoryBindingDescriptor
    : MessagingDescriptorBase<InMemoryBindingConfiguration>
    , IInMemoryBindingDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBindingDescriptor"/> class.
    /// </summary>
    public InMemoryBindingDescriptor(IMessagingConfigurationContext context, string source, string destination)
        : base(context)
    {
        Configuration = new InMemoryBindingConfiguration { Source = source, Destination = destination };
    }

    /// <inheritdoc />
    protected override InMemoryBindingConfiguration Configuration { get; set; }

    /// <inheritdoc />
    public IInMemoryBindingDescriptor Source(string topicName)
    {
        Configuration.Source = topicName;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryBindingDescriptor ToQueue(string queueName)
    {
        Configuration.Destination = queueName;
        Configuration.DestinationKind = InMemoryDestinationKind.Queue;
        return this;
    }

    /// <inheritdoc />
    public IInMemoryBindingDescriptor ToTopic(string topicName)
    {
        Configuration.Destination = topicName;
        Configuration.DestinationKind = InMemoryDestinationKind.Topic;
        return this;
    }

    /// <summary>
    /// Creates the final binding configuration.
    /// </summary>
    /// <returns>The configured binding configuration.</returns>
    public InMemoryBindingConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new binding descriptor with the specified source and destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="source">The source topic name.</param>
    /// <param name="destination">The destination queue or topic name.</param>
    /// <returns>A new binding descriptor.</returns>
    public static InMemoryBindingDescriptor New(
        IMessagingConfigurationContext context,
        string source,
        string destination)
        => new(context, source, destination);
}
