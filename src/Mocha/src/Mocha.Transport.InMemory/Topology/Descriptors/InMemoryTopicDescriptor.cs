namespace Mocha.Transport.InMemory;

/// <summary>
/// Descriptor implementation for configuring a InMemory topic.
/// </summary>
internal sealed class InMemoryTopicDescriptor
    : MessagingDescriptorBase<InMemoryTopicConfiguration>
    , IInMemoryTopicDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTopicDescriptor"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The initial topic name.</param>
    public InMemoryTopicDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new InMemoryTopicConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override InMemoryTopicConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IInMemoryTopicDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <summary>
    /// Creates the final topic configuration.
    /// </summary>
    /// <returns>The configured topic configuration.</returns>
    public InMemoryTopicConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new topic descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The topic name.</param>
    /// <returns>A new topic descriptor.</returns>
    public static InMemoryTopicDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
